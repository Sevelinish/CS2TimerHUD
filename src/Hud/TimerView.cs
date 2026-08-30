using System.Drawing;

namespace TimerHud.Hud;

public readonly record struct TimerView(string MainText, Color MainColor, string PreviousText, Color PreviousColor)
{
    public bool HasPrevious => PreviousText.Length > 0;
}
