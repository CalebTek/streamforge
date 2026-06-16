using StreamForge.Core.Encoding;
using Xunit;

namespace StreamForge.Tests;

public sealed class FfmpegProgressParserTests
{
    [Fact]
    public void Constructor_ZeroDuration_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new FfmpegProgressParser(0));

    [Fact]
    public void Constructor_NegativeDuration_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new FfmpegProgressParser(-1));

    [Fact]
    public void InitialFraction_IsZero()
    {
        var parser = new FfmpegProgressParser(100);
        Assert.Equal(0.0, parser.Fraction);
    }

    [Fact]
    public void Feed_OutTimeUs_UpdatesFraction()
    {
        var parser = new FfmpegProgressParser(100);  // 100 s = 100_000_000 us
        parser.Feed("out_time_us=50000000");          // 50 s
        Assert.Equal(0.5, parser.Fraction, precision: 6);
    }

    [Fact]
    public void Feed_OutTimeMs_UpdatesFraction()
    {
        var parser = new FfmpegProgressParser(100);  // 100 s = 100_000_000 us
        parser.Feed("out_time_ms=75000000");          // 75 s worth of ms (value is in us)
        Assert.Equal(0.75, parser.Fraction, precision: 6);
    }

    [Fact]
    public void Feed_ProgressEnd_SetsIsCompleteAndFractionOne()
    {
        var parser = new FfmpegProgressParser(100);
        parser.Feed("progress=end");
        Assert.True(parser.IsComplete);
        Assert.Equal(1.0, parser.Fraction);
    }

    [Fact]
    public void Feed_ExceedsDuration_ClampedToOne()
    {
        var parser = new FfmpegProgressParser(100);
        parser.Feed("out_time_us=200000000");   // 200 s > 100 s
        Assert.Equal(1.0, parser.Fraction);
        Assert.False(parser.IsComplete);
    }

    [Fact]
    public void Feed_LineWithoutEquals_Ignored()
    {
        var parser = new FfmpegProgressParser(100);
        var fraction = parser.Feed("frame=42");   // no '=' at all
        Assert.Equal(0.0, fraction);
    }

    [Fact]
    public void Feed_UnknownKey_Ignored()
    {
        var parser = new FfmpegProgressParser(100);
        parser.Feed("fps=30");
        Assert.Equal(0.0, parser.Fraction);
    }

    [Fact]
    public void Feed_ProgressContinue_DoesNotSetComplete()
    {
        var parser = new FfmpegProgressParser(100);
        parser.Feed("progress=continue");
        Assert.False(parser.IsComplete);
    }

    [Fact]
    public void Feed_SequentialUpdates_FractionIncreases()
    {
        var parser = new FfmpegProgressParser(100);
        parser.Feed("out_time_us=10000000");
        var f1 = parser.Fraction;
        parser.Feed("out_time_us=60000000");
        var f2 = parser.Fraction;
        Assert.True(f2 > f1);
        Assert.Equal(0.6, f2, precision: 6);
    }

    [Fact]
    public void Feed_ReturnsCurrentFraction()
    {
        var parser = new FfmpegProgressParser(200);
        var result = parser.Feed("out_time_us=100000000");  // 100s of 200s = 0.5
        Assert.Equal(0.5, result, precision: 6);
    }
}
