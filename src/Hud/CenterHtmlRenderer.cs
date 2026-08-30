using System.Drawing;
using System.Text;
using TimerHud.Players;

namespace TimerHud.Hud;

public sealed class CenterHtmlRenderer : ITimerRenderer
{
    private readonly TimerTheme _theme;
    private readonly StringBuilder _builder = new(256);

    public CenterHtmlRenderer(TimerTheme theme)
    {
        _theme = theme;
    }

    public TimerRenderMode Mode => TimerRenderMode.CenterHtml;

    public bool RequiresContinuousRefresh => true;

    public bool Render(TimerSession session, TimerView view, int tick)
    {
        var controller = session.Controller;
        if (!controller.IsValid)
            return false;

        controller.PrintToCenterHtml(BuildMarkup(view));
        return true;
    }

    public void Clear(TimerSession session)
    {
        if (session.Controller.IsValid)
            session.Controller.PrintToCenterHtml(" ");
    }

    private string BuildMarkup(TimerView view)
    {
        _builder.Clear();

        Append(_theme.HtmlFontClass, view.MainColor, view.MainText);

        if (view.HasPrevious)
        {
            _builder.Append("<br>");
            Append(_theme.HtmlPreviousFontClass, view.PreviousColor, view.PreviousText);
        }

        return _builder.ToString();
    }

    private void Append(string fontClass, Color color, string text) =>
        _builder
            .Append("<font class='")
            .Append(fontClass)
            .Append("' color='#")
            .Append($"{color.R:X2}{color.G:X2}{color.B:X2}")
            .Append("'>")
            .Append(text)
            .Append("</font>");
}
