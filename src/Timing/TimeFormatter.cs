using System.Globalization;
using System.Text;

namespace TimerHud.Timing;

public sealed class TimeFormatter
{
    private readonly float _tickInterval;
    private readonly int _precision;
    private readonly bool _alwaysShowHours;
    private readonly StringBuilder _builder = new(24);

    public TimeFormatter(float tickInterval, int precision, bool alwaysShowHours)
    {
        _tickInterval = tickInterval > 0f ? tickInterval : 1f / 64f;
        _precision = Math.Clamp(precision, 0, 3);
        _alwaysShowHours = alwaysShowHours;
    }

    public int TicksFromSeconds(float seconds) =>
        seconds <= 0f ? 0 : (int)Math.Ceiling(seconds / _tickInterval);

    public string Format(int ticks)
    {
        if (ticks < 0)
            ticks = 0;

        var totalMilliseconds = (long)Math.Round(ticks * (double)_tickInterval * 1000.0);

        var hours = totalMilliseconds / 3600000L;
        var minutes = totalMilliseconds / 60000L % 60L;
        var seconds = totalMilliseconds / 1000L % 60L;
        var milliseconds = totalMilliseconds % 1000L;

        _builder.Clear();

        if (hours > 0L || _alwaysShowHours)
        {
            _builder.Append(hours.ToString(CultureInfo.InvariantCulture));
            _builder.Append(':');
            _builder.Append(minutes.ToString("00", CultureInfo.InvariantCulture));
        }
        else
        {
            _builder.Append(minutes.ToString("00", CultureInfo.InvariantCulture));
        }

        _builder.Append(':');
        _builder.Append(seconds.ToString("00", CultureInfo.InvariantCulture));

        if (_precision > 0)
        {
            var fraction = _precision switch
            {
                1 => milliseconds / 100L,
                2 => milliseconds / 10L,
                _ => milliseconds,
            };

            _builder.Append('.');
            _builder.Append(fraction.ToString(new string('0', _precision), CultureInfo.InvariantCulture));
        }

        return _builder.ToString();
    }
}
