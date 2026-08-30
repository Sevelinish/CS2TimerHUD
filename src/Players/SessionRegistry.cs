namespace TimerHud.Players;

public sealed class SessionRegistry
{
    public const int MaxSlots = 68;
    private readonly TimerSession?[] _sessions = new TimerSession?[MaxSlots];
    public int Count { get; private set; }

    public TimerSession? Get(int slot) =>
        (uint)slot < MaxSlots ? _sessions[slot] : null;

    public void Add(TimerSession session)
    {
        if ((uint)session.Slot >= MaxSlots)
            return;

        if (_sessions[session.Slot] is null)
            Count++;

        _sessions[session.Slot] = session;
    }

    public TimerSession? Remove(int slot)
    {
        if ((uint)slot >= MaxSlots)
            return null;

        var session = _sessions[slot];
        if (session is not null)
        {
            _sessions[slot] = null;
            Count--;
        }

        return session;
    }

    public List<TimerSession> Snapshot()
    {
        var result = new List<TimerSession>(Count);
        for (var slot = 0; slot < MaxSlots; slot++)
        {
            if (_sessions[slot] is { } session)
                result.Add(session);
        }

        return result;
    }

    public void Clear()
    {
        Array.Clear(_sessions, 0, MaxSlots);
        Count = 0;
    }

    public Enumerator GetEnumerator() => new(_sessions);

    public struct Enumerator
    {
        private readonly TimerSession?[] _sessions;
        private int _index;

        internal Enumerator(TimerSession?[] sessions)
        {
            _sessions = sessions;
            _index = -1;
            Current = null!;
        }

        public TimerSession Current { get; private set; }

        public bool MoveNext()
        {
            while (++_index < MaxSlots)
            {
                if (_sessions[_index] is { } session)
                {
                    Current = session;
                    return true;
                }
            }

            return false;
        }
    }
}
