namespace TimerHud.Timing;

public sealed class RunTimer
{
    private int _accumulatedTicks;
    private int _startTick;

    public TimerState State { get; private set; } = TimerState.Idle;

    public int? PreviousTicks { get; private set; }

    public int ElapsedTicks(int tick)
    {
        if (State != TimerState.Running)
            return _accumulatedTicks;

        var delta = tick - _startTick;
        return delta > 0 ? _accumulatedTicks + delta : _accumulatedTicks;
    }

    public TimerAction Advance(int tick, int minimumRecordTicks)
    {
        switch (State)
        {
            case TimerState.Idle:
                _startTick = tick;
                State = TimerState.Running;
                return TimerAction.Started;

            case TimerState.Running:
                _accumulatedTicks = ElapsedTicks(tick);
                State = TimerState.Paused;
                return TimerAction.Paused;

            default:
                if (_accumulatedTicks >= minimumRecordTicks)
                    PreviousTicks = _accumulatedTicks;

                _accumulatedTicks = 0;
                State = TimerState.Idle;
                return TimerAction.Overwritten;
        }
    }

    public void Rebase(int tick)
    {
        if (State == TimerState.Running)
            _startTick = tick;
    }

    public void Reset(bool keepPrevious)
    {
        _accumulatedTicks = 0;
        _startTick = 0;
        State = TimerState.Idle;

        if (!keepPrevious)
            PreviousTicks = null;
    }
}
