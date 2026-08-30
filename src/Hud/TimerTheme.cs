using System.Drawing;
using System.Globalization;
using TimerHud.Configuration;
using TimerHud.Timing;

namespace TimerHud.Hud;

public sealed class TimerTheme
{
    public TimerTheme(TimerThemeConfig config)
    {
        Idle = ParseColor(config.IdleColor, Color.FromArgb(156, 163, 175));
        Running = ParseColor(config.RunningColor, Color.FromArgb(74, 222, 128));
        Paused = ParseColor(config.PausedColor, Color.FromArgb(250, 204, 21));
        Previous = ParseColor(config.PreviousColor, Color.FromArgb(107, 114, 128));

        FontName = config.FontName;
        FontSize = config.FontSize;
        PreviousFontSize = config.PreviousFontSize;
        PreviousLabel = config.PreviousLabel;
        HtmlFontClass = config.HtmlFontClass;
        HtmlPreviousFontClass = config.HtmlPreviousFontClass;
        DrawBackground = config.DrawBackground;
        BackgroundPaddingX = config.BackgroundPaddingX;
        BackgroundPaddingY = config.BackgroundPaddingY;
    }

    public Color Idle { get; }
    public Color Running { get; }
    public Color Paused { get; }
    public Color Previous { get; }
    public string FontName { get; }
    public float FontSize { get; }
    public float PreviousFontSize { get; }
    public string PreviousLabel { get; }
    public string HtmlFontClass { get; }
    public string HtmlPreviousFontClass { get; }
    public bool DrawBackground { get; }
    public float BackgroundPaddingX { get; }
    public float BackgroundPaddingY { get; }

    public Color ColorFor(TimerState state) => state switch
    {
        TimerState.Running => Running,
        TimerState.Paused => Paused,
        _ => Idle,
    };

    public static Color ParseColor(string? raw, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        var value = raw.Trim().TrimStart('#');

        if (value.Length is 6 or 8 &&
            uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packed))
        {
            return value.Length == 6
                ? Color.FromArgb(255, (byte)(packed >> 16), (byte)(packed >> 8), (byte)packed)
                : Color.FromArgb((byte)(packed >> 24), (byte)(packed >> 16), (byte)(packed >> 8), (byte)packed);
        }

        var named = Color.FromName(raw.Trim());
        return named.IsKnownColor ? named : fallback;
    }
}
