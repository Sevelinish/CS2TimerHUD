namespace TimerHud.Hud;

public enum TimerRenderMode
{
    WorldText,

    CenterHtml,
}

public static class TimerRenderModeParser
{
    public static TimerRenderMode Parse(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "centerhtml" or "html" or "center" => TimerRenderMode.CenterHtml,
        _ => TimerRenderMode.WorldText,
    };
}
