using System.Text.Json.Serialization;

namespace TimerHud.Players;

public sealed class PlayerPreferences
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("button")]
    public string? Button { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    public bool IsDefault => Enabled is null && Button is null && Language is null;

    public void Reset()
    {
        Enabled = null;
        Button = null;
        Language = null;
    }
}
