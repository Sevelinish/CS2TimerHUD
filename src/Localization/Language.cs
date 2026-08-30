namespace TimerHud.Localization;

public enum Language
{
    English,
    Russian,
    German,
    Ukrainian,
    Spanish,
}

public static class LanguageParser
{
    public static Language Parse(string? raw, Language fallback = Language.English)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        var value = raw.Trim().ToLowerInvariant();

        if (value.StartsWith("css_", StringComparison.Ordinal))
            value = value[4..];

        if (value.StartsWith("timerhud_", StringComparison.Ordinal))
            value = value[9..];

        return value switch
        {
            "rus" or "ru" or "russian" => Language.Russian,
            "deutsch" or "de" or "ger" or "german" => Language.German,
            "ukr" or "ua" or "uk" or "ukrainian" => Language.Ukrainian,
            "spanish" or "es" or "esp" or "espanol" => Language.Spanish,
            "engl" or "en" or "eng" or "english" => Language.English,
            _ => fallback,
        };
    }

    public static string ToConfigValue(this Language language) => language switch
    {
        Language.Russian => "rus",
        Language.German => "deutsch",
        Language.Ukrainian => "ukr",
        Language.Spanish => "spanish",
        _ => "engl",
    };
}
