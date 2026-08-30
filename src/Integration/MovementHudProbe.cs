using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using TimerHud.Configuration;
using TimerHud.Hud;

namespace TimerHud.Integration;

public readonly record struct MovementHudPlacement(float TopEdgeAngle);

public sealed class MovementHudProbe
{
    private const string WorldTextClassName = "point_worldtext";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly TimerHudConfig _config;
    private readonly ILogger _logger;
    private readonly string _configPath;
    private readonly string _preferencesPath;
    private readonly HashSet<uint> _occupiedViewModels = new();

    private MovementHudSnapshot _snapshot = MovementHudSnapshot.Empty;
    private DateTime _configStamp = DateTime.MinValue;
    private DateTime _preferencesStamp = DateTime.MinValue;
    private bool _scanFailureLogged;

    public MovementHudProbe(TimerHudConfig config, string moduleDirectory, ILogger logger)
    {
        _config = config;
        _logger = logger;

        var integration = config.MovementHud;
        var pluginsRoot = Directory.GetParent(moduleDirectory)?.FullName ?? moduleDirectory;
        var sharpRoot = Directory.GetParent(pluginsRoot)?.FullName ?? pluginsRoot;

        _configPath = string.IsNullOrWhiteSpace(integration.ConfigPath)
            ? Path.Combine(sharpRoot, "configs", "plugins", integration.PluginName, integration.PluginName + ".json")
            : integration.ConfigPath;

        _preferencesPath = string.IsNullOrWhiteSpace(integration.PreferencesPath)
            ? Path.Combine(pluginsRoot, integration.PluginName, "data", "preferences.json")
            : integration.PreferencesPath;
    }

    public MovementHudDetection Detection => MovementHudDetectionParser.Parse(_config.MovementHud.Detection);

    public string ConfigPath => _configPath;

    public string PreferencesPath => _preferencesPath;

    public bool ConfigLoaded => _snapshot.ConfigLoaded;

    public int OccupiedViewModels => _occupiedViewModels.Count;

    public void RefreshFiles()
    {
        var configFile = new FileInfo(_configPath);
        var preferencesFile = new FileInfo(_preferencesPath);

        var configStamp = configFile.Exists ? configFile.LastWriteTimeUtc : DateTime.MinValue;
        var preferencesStamp = preferencesFile.Exists ? preferencesFile.LastWriteTimeUtc : DateTime.MinValue;

        if (configStamp == _configStamp && preferencesStamp == _preferencesStamp)
            return;

        _configStamp = configStamp;
        _preferencesStamp = preferencesStamp;

        var parsed = ReadConfig(configFile);
        var players = ReadPreferences(preferencesFile);
        var integration = _config.MovementHud;

        if (parsed is null)
        {
            _snapshot = new MovementHudSnapshot
            {
                ConfigLoaded = false,
                Rows = integration.AssumedRows,
                OffsetY = integration.AssumedOffsetY,
                RowSpacing = integration.AssumedRowSpacing,
                FontSize = integration.AssumedFontSize,
                Scale = integration.AssumedScale,
                Distance = integration.AssumedDistance,
                Players = players,
            };

            return;
        }

        _snapshot = new MovementHudSnapshot
        {
            ConfigLoaded = true,
            Enabled = parsed.Enabled ?? true,
            EnabledByDefault = parsed.EnabledByDefault ?? true,
            WorldTextByDefault = IsWorldText(parsed.DefaultRenderMode),
            Rows = RowsFor(parsed.DiagonalMode),
            OffsetY = parsed.WorldText?.OffsetY ?? integration.AssumedOffsetY,
            RowSpacing = parsed.WorldText?.RowSpacing ?? integration.AssumedRowSpacing,
            FontSize = parsed.Theme?.FontSize ?? integration.AssumedFontSize,
            Scale = parsed.WorldText?.Scale ?? integration.AssumedScale,
            Distance = parsed.WorldText?.Distance ?? integration.AssumedDistance,
            Players = players,
        };
    }

    public void ScanEntities(HashSet<uint> ownEntities)
    {
        try
        {
            _occupiedViewModels.Clear();

            foreach (var text in Utilities.FindAllEntitiesByDesignerName<CPointWorldText>(WorldTextClassName))
            {
                if (!text.IsValid || ownEntities.Contains(text.Index))
                    continue;

                var node = text.CBodyComponent?.SceneNode;
                if (node is null || node.Handle == IntPtr.Zero)
                    continue;

                var parent = node.PParent;
                if (parent is null || parent.Handle == IntPtr.Zero)
                    continue;

                var owner = parent.Owner;
                if (owner is null || !owner.IsValid)
                    continue;

                _occupiedViewModels.Add(owner.Index);
            }

            _scanFailureLogged = false;
        }
        catch (Exception exception)
        {
            _occupiedViewModels.Clear();

            if (_scanFailureLogged)
                return;

            _scanFailureLogged = true;
            _logger.LogError(exception, "TimerHUD: world text probe failed, falling back to the solo layout.");
        }
    }

    public bool IsStacked(ulong steamId, uint viewModelIndex) => Detection switch
    {
        MovementHudDetection.Always => true,
        MovementHudDetection.Never => false,
        MovementHudDetection.Files => _snapshot.IsEnabledFor(steamId),
        _ => viewModelIndex != 0 && _occupiedViewModels.Contains(viewModelIndex),
    };

    public MovementHudPlacement Placement(ulong steamId)
    {
        var player = _snapshot.PlayerFor(steamId);

        var offsetY = player?.OffsetY ?? _snapshot.OffsetY;
        var scale = player?.Scale ?? _snapshot.Scale;
        var angle = HudScale.LineAngle(_snapshot.FontSize, scale, _snapshot.Distance);
        var verticalCenter = (_snapshot.Rows - 1) / 2f;

        return new MovementHudPlacement((offsetY + verticalCenter * _snapshot.RowSpacing + 0.5f) * angle);
    }

    private MovementHudConfigFile? ReadConfig(FileInfo file)
    {
        if (!file.Exists)
            return null;

        try
        {
            return JsonSerializer.Deserialize<MovementHudConfigFile>(File.ReadAllText(file.FullName), SerializerOptions);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "TimerHUD: could not read {Path}, assumed geometry is used.", file.FullName);
            return null;
        }
    }

    private Dictionary<ulong, MovementHudPlayerFile> ReadPreferences(FileInfo file)
    {
        if (!file.Exists)
            return new Dictionary<ulong, MovementHudPlayerFile>();

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<ulong, MovementHudPlayerFile>>(
                File.ReadAllText(file.FullName), SerializerOptions);

            return data ?? new Dictionary<ulong, MovementHudPlayerFile>();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "TimerHUD: could not read {Path}, server defaults are used.", file.FullName);
            return new Dictionary<ulong, MovementHudPlayerFile>();
        }
    }

    private static bool IsWorldText(string? mode) =>
        string.IsNullOrWhiteSpace(mode) || mode.Trim().ToLowerInvariant() is not ("centerhtml" or "html" or "center");

    private static int RowsFor(string? diagonalMode) => diagonalMode?.Trim().ToLowerInvariant() switch
    {
        "full" or "all" => 4,
        _ => 3,
    };
}
