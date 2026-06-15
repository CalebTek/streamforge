using System.Globalization;

namespace StreamForge.Core.Encoding;

public sealed class FfmpegProgressParser
{
    private readonly long _totalDurationUs;
    private long _lastOutTimeUs;

    public FfmpegProgressParser(double totalDurationSeconds)
    {
        if (totalDurationSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalDurationSeconds));
        _totalDurationUs = (long)(totalDurationSeconds * 1_000_000);
    }

    public bool IsComplete { get; private set; }

    public double Fraction =>
        IsComplete ? 1.0 : Math.Clamp((double)_lastOutTimeUs / _totalDurationUs, 0.0, 1.0);

    public double Feed(string line)
    {
        var idx = line.IndexOf('=');
        if (idx <= 0) return Fraction;
        var key = line[..idx].Trim();
        var value = line[(idx + 1)..].Trim();
        switch (key)
        {
            case "out_time_us" or "out_time_ms":
                if (long.TryParse(value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var us))
                    _lastOutTimeUs = us;
                break;
            case "progress" when value == "end":
                IsComplete = true;
                break;
        }
        return Fraction;
    }
}
