using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TimerHud.Players;

public sealed class PreferenceStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _path;
    private readonly ILogger _logger;
    private readonly Dictionary<ulong, PlayerPreferences> _preferences = new();
    private readonly object _gate = new();
    private volatile bool _dirty;

    public PreferenceStore(string path, ILogger logger)
    {
        _path = path;
        _logger = logger;
    }

    public PlayerPreferences GetOrCreate(ulong steamId)
    {
        lock (_gate)
        {
            if (!_preferences.TryGetValue(steamId, out var preferences))
            {
                preferences = new PlayerPreferences();
                _preferences[steamId] = preferences;
            }

            return preferences;
        }
    }

    public void MarkDirty() => _dirty = true;

    public void Load()
    {
        try
        {
            if (!File.Exists(_path))
                return;

            var data = JsonSerializer.Deserialize<Dictionary<ulong, PlayerPreferences>>(File.ReadAllText(_path));
            if (data is null)
                return;

            lock (_gate)
            {
                _preferences.Clear();
                foreach (var entry in data)
                    _preferences[entry.Key] = entry.Value;
            }

            _logger.LogInformation("TimerHUD: loaded {Count} stored player settings.", data.Count);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "TimerHUD: could not read {Path}, defaults are used.", _path);
        }
    }

    public void Flush(bool force = false)
    {
        if (!_dirty && !force)
            return;

        _dirty = false;

        try
        {
            Dictionary<ulong, PlayerPreferences> snapshot;
            lock (_gate)
            {
                snapshot = _preferences
                    .Where(pair => !pair.Value.IsDefault)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(snapshot, SerializerOptions));
            File.Move(temporaryPath, _path, overwrite: true);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "TimerHUD: could not write settings to {Path}.", _path);
        }
    }
}
