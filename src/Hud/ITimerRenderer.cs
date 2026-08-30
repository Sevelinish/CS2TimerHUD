using TimerHud.Players;

namespace TimerHud.Hud;

public interface ITimerRenderer
{
    TimerRenderMode Mode { get; }

    bool RequiresContinuousRefresh { get; }

    bool Render(TimerSession session, TimerView view, int tick);

    void Clear(TimerSession session);
}
