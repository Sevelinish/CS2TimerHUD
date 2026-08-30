using System.Text.Json.Serialization;

namespace TimerHud.Integration;

public sealed class MovementHudSnapshot
{
    public static readonly MovementHudSnapshot Empty = new();

    public bool ConfigLoaded { get; init; }
    public bool Enabled { get; init; } = true;
    public bool EnabledByDefault { get; init; } = true;
    public bool WorldTextByDefault { get; init; } = true;
    public int Rows { get; init; } = 3;
    public float OffsetY { get; init; } = -5f;
    public float RowSpacing { get; init; } = 1.35f;
    public float FontSize { get; init; } = 32f;
    public float Scale { get; init; } = 1f;
    public float Distance { get; init; } = 7f;
    public IReadOnlyDictionary<ulong, MovementHudPlayerFile> Players { get; init; } =
        new Dictionary<ulong, MovementHudPlayerFile>();

    public MovementHudPlayerFile? PlayerFor(ulong steamId) =>
        Players.TryGetValue(steamId, out var player) ? player : null;

    public bool IsWorldTextFor(ulong steamId)
    {
        var mode = PlayerFor(steamId)?.Mode;
        if (string.IsNullOrWhiteSpace(mode))
            return WorldTextByDefault;

        return mode.Trim().ToLowerInvariant() is not ("centerhtml" or "html" or "center");
    }

    public bool IsEnabledFor(ulong steamId) =>
        Enabled && (PlayerFor(steamId)?.Enabled ?? EnabledByDefault) && IsWorldTextFor(steamId);
}

public sealed class MovementHudConfigFile
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("enabled_by_default")]
    public bool? EnabledByDefault { get; set; }

    [JsonPropertyName("default_render_mode")]
    public string? DefaultRenderMode { get; set; }

    [JsonPropertyName("diagonal_mode")]
    public string? DiagonalMode { get; set; }

    [JsonPropertyName("theme")]
    public MovementHudThemeFile? Theme { get; set; }

    [JsonPropertyName("world_text")]
    public MovementHudWorldTextFile? WorldText { get; set; }
}

public sealed class MovementHudThemeFile
{
    [JsonPropertyName("font_size")]
    public float? FontSize { get; set; }
}

public sealed class MovementHudWorldTextFile
{
    [JsonPropertyName("distance")]
    public float? Distance { get; set; }

    [JsonPropertyName("offset_y")]
    public float? OffsetY { get; set; }

    [JsonPropertyName("row_spacing")]
    public float? RowSpacing { get; set; }

    [JsonPropertyName("scale")]
    public float? Scale { get; set; }
}

public sealed class MovementHudPlayerFile
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("offset_y")]
    public float? OffsetY { get; set; }

    [JsonPropertyName("scale")]
    public float? Scale { get; set; }
}
