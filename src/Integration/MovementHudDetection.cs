namespace TimerHud.Integration;

public enum MovementHudDetection
{
    Entities,
    Files,
    Always,
    Never,
}

public static class MovementHudDetectionParser
{
    public static MovementHudDetection Parse(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "files" or "config" or "preferences" => MovementHudDetection.Files,
        "always" or "on" or "true" or "stacked" => MovementHudDetection.Always,
        "never" or "off" or "false" or "solo" => MovementHudDetection.Never,
        _ => MovementHudDetection.Entities,
    };

    public static string ToConfigValue(this MovementHudDetection detection) => detection switch
    {
        MovementHudDetection.Files => "files",
        MovementHudDetection.Always => "always",
        MovementHudDetection.Never => "never",
        _ => "entities",
    };
}
