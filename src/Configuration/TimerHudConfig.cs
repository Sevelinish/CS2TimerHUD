using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace TimerHud.Configuration;

public sealed class TimerHudConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 1;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("enabled_by_default")]
    public bool EnabledByDefault { get; set; } = true;

    [JsonPropertyName("allow_player_toggle")]
    public bool AllowPlayerToggle { get; set; } = true;

    [JsonPropertyName("default_language")]
    public string DefaultLanguage { get; set; } = "engl";

    [JsonPropertyName("default_button")]
    public string DefaultButton { get; set; } = "";

    [JsonPropertyName("chat_triggers")]
    public string ChatTriggers { get; set; } = "!/";

    [JsonPropertyName("default_render_mode")]
    public string DefaultRenderMode { get; set; } = "worldtext";

    [JsonPropertyName("fallback_to_center_html")]
    public bool FallbackToCenterHtml { get; set; } = true;

    [JsonPropertyName("update_interval")]
    public int UpdateInterval { get; set; } = 2;

    [JsonPropertyName("time_precision")]
    public int TimePrecision { get; set; } = 2;

    [JsonPropertyName("always_show_hours")]
    public bool AlwaysShowHours { get; set; } = false;

    [JsonPropertyName("minimum_record_seconds")]
    public float MinimumRecordSeconds { get; set; } = 0.15f;

    [JsonPropertyName("reset_on_map_change")]
    public bool ResetOnMapChange { get; set; } = true;

    [JsonPropertyName("announce_actions")]
    public bool AnnounceActions { get; set; } = false;

    [JsonPropertyName("text_update_mode")]
    public string TextUpdateMode { get; set; } = "both";

    [JsonPropertyName("theme")]
    public TimerThemeConfig Theme { get; set; } = new();

    [JsonPropertyName("layout")]
    public TimerLayoutConfig Layout { get; set; } = new();

    [JsonPropertyName("movement_hud")]
    public MovementHudIntegrationConfig MovementHud { get; set; } = new();

    public void Normalize()
    {
        UpdateInterval = Math.Clamp(UpdateInterval, 1, 16);
        TimePrecision = Math.Clamp(TimePrecision, 0, 3);
        MinimumRecordSeconds = Math.Clamp(MinimumRecordSeconds, 0f, 60f);

        if (string.IsNullOrWhiteSpace(ChatTriggers))
            ChatTriggers = "!/";

        Theme.Normalize();
        Layout.Normalize();
        MovementHud.Normalize();
    }
}

public sealed class TimerThemeConfig
{
    [JsonPropertyName("idle_color")]
    public string IdleColor { get; set; } = "#9CA3AF";

    [JsonPropertyName("running_color")]
    public string RunningColor { get; set; } = "#4ADE80";

    [JsonPropertyName("paused_color")]
    public string PausedColor { get; set; } = "#FACC15";

    [JsonPropertyName("previous_color")]
    public string PreviousColor { get; set; } = "#6B7280";

    [JsonPropertyName("font_name")]
    public string FontName { get; set; } = "Arial Black";

    [JsonPropertyName("font_size")]
    public float FontSize { get; set; } = 34f;

    [JsonPropertyName("previous_font_size")]
    public float PreviousFontSize { get; set; } = 22f;

    [JsonPropertyName("previous_label")]
    public string PreviousLabel { get; set; } = "TMP ";

    [JsonPropertyName("html_font_class")]
    public string HtmlFontClass { get; set; } = "fontSize-m";

    [JsonPropertyName("html_previous_font_class")]
    public string HtmlPreviousFontClass { get; set; } = "fontSize-sm";

    [JsonPropertyName("draw_background")]
    public bool DrawBackground { get; set; } = false;

    [JsonPropertyName("background_padding_x")]
    public float BackgroundPaddingX { get; set; } = 0.25f;

    [JsonPropertyName("background_padding_y")]
    public float BackgroundPaddingY { get; set; } = 0.15f;

    public void Normalize()
    {
        FontSize = Math.Clamp(FontSize, 8f, 128f);
        PreviousFontSize = Math.Clamp(PreviousFontSize, 8f, 128f);
        BackgroundPaddingX = Math.Clamp(BackgroundPaddingX, 0f, 4f);
        BackgroundPaddingY = Math.Clamp(BackgroundPaddingY, 0f, 4f);

        if (string.IsNullOrWhiteSpace(FontName)) FontName = "Arial Black";
        if (string.IsNullOrWhiteSpace(HtmlFontClass)) HtmlFontClass = "fontSize-m";
        if (string.IsNullOrWhiteSpace(HtmlPreviousFontClass)) HtmlPreviousFontClass = "fontSize-sm";

        PreviousLabel ??= string.Empty;
    }
}

public sealed class TimerLayoutConfig
{
    [JsonPropertyName("distance")]
    public float Distance { get; set; } = 7f;

    [JsonPropertyName("offset_x")]
    public float OffsetX { get; set; } = 0f;

    [JsonPropertyName("solo_offset_y")]
    public float SoloOffsetY { get; set; } = -5f;

    [JsonPropertyName("row_spacing")]
    public float RowSpacing { get; set; } = 1.35f;

    [JsonPropertyName("solo_scale")]
    public float SoloScale { get; set; } = 1f;

    [JsonPropertyName("stacked_scale")]
    public float StackedScale { get; set; } = 0.75f;

    [JsonPropertyName("stack_gap")]
    public float StackGap { get; set; } = 0.6f;

    public void Normalize()
    {
        Distance = Math.Clamp(Distance, 2f, 40f);
        OffsetX = Math.Clamp(OffsetX, -40f, 40f);
        SoloOffsetY = Math.Clamp(SoloOffsetY, -40f, 40f);
        RowSpacing = Math.Clamp(RowSpacing, 0.2f, 10f);
        SoloScale = Math.Clamp(SoloScale, 0.1f, 5f);
        StackedScale = Math.Clamp(StackedScale, 0.1f, 5f);
        StackGap = Math.Clamp(StackGap, 0f, 20f);
    }
}

public sealed class MovementHudIntegrationConfig
{
    [JsonPropertyName("detection")]
    public string Detection { get; set; } = "entities";

    [JsonPropertyName("plugin_name")]
    public string PluginName { get; set; } = "MovementHUD";

    [JsonPropertyName("config_path")]
    public string ConfigPath { get; set; } = "";

    [JsonPropertyName("preferences_path")]
    public string PreferencesPath { get; set; } = "";

    [JsonPropertyName("file_refresh_interval")]
    public float FileRefreshInterval { get; set; } = 5f;

    [JsonPropertyName("entity_probe_interval")]
    public float EntityProbeInterval { get; set; } = 1f;

    [JsonPropertyName("assumed_rows")]
    public int AssumedRows { get; set; } = 3;

    [JsonPropertyName("assumed_offset_y")]
    public float AssumedOffsetY { get; set; } = -5f;

    [JsonPropertyName("assumed_row_spacing")]
    public float AssumedRowSpacing { get; set; } = 1.35f;

    [JsonPropertyName("assumed_font_size")]
    public float AssumedFontSize { get; set; } = 32f;

    [JsonPropertyName("assumed_scale")]
    public float AssumedScale { get; set; } = 1f;

    [JsonPropertyName("assumed_distance")]
    public float AssumedDistance { get; set; } = 7f;

    public void Normalize()
    {
        FileRefreshInterval = Math.Clamp(FileRefreshInterval, 1f, 300f);
        EntityProbeInterval = Math.Clamp(EntityProbeInterval, 0.2f, 60f);
        AssumedRows = Math.Clamp(AssumedRows, 1, 12);
        AssumedOffsetY = Math.Clamp(AssumedOffsetY, -40f, 40f);
        AssumedRowSpacing = Math.Clamp(AssumedRowSpacing, 0.2f, 10f);
        AssumedFontSize = Math.Clamp(AssumedFontSize, 8f, 128f);
        AssumedScale = Math.Clamp(AssumedScale, 0.1f, 5f);
        AssumedDistance = Math.Clamp(AssumedDistance, 2f, 40f);

        if (string.IsNullOrWhiteSpace(PluginName)) PluginName = "MovementHUD";
    }
}
