using StreamForge.Core.Encoding;
using StreamForge.Core.Models;
using Xunit;

namespace StreamForge.Tests;

public sealed class FfmpegCommandBuilderTests
{
    private static JobRequest OneRendition(string name = "720p", int w = 1280, int h = 720, int kbps = 2500) =>
        new() { SourceUrl = "input.mp4", Outputs = [new OutputRendition { Name = name, Width = w, Height = h, VideoBitrateKbps = kbps }] };

    private static JobRequest TwoRenditions() => new()
    {
        SourceUrl = "input.mp4",
        Outputs =
        [
            new OutputRendition { Name = "1080p", Width = 1920, Height = 1080, VideoBitrateKbps = 4500 },
            new OutputRendition { Name = "720p",  Width = 1280, Height = 720,  VideoBitrateKbps = 2500 }
        ]
    };

    [Fact]
    public void EmptyOutputs_ThrowsArgumentException()
    {
        var request = new JobRequest { SourceUrl = "x", Outputs = [] };
        Assert.Throws<ArgumentException>(() =>
            FfmpegCommandBuilder.BuildHlsArgs(request, "input.mp4", "/out"));
    }

    [Fact]
    public void SingleRendition_ContainsInputFlag()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "input.mp4", "/out");
        var list = args.ToList();
        var idx = list.IndexOf("-i");
        Assert.True(idx >= 0);
        Assert.Equal("input.mp4", list[idx + 1]);
    }

    [Fact]
    public void SingleRendition_ContainsProgressPipe()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "input.mp4", "/out");
        var list = args.ToList();
        var idx = list.IndexOf("-progress");
        Assert.True(idx >= 0);
        Assert.Equal("pipe:1", list[idx + 1]);
    }

    [Fact]
    public void SingleRendition_ContainsCorrectBitrateAndScale()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "input.mp4", "/out").ToList();
        Assert.Contains("2500k", args);
        Assert.Contains("scale=1280:720", args);
    }

    [Fact]
    public void TwoRenditions_HasTwoBitrateEntries()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(TwoRenditions(), "input.mp4", "/out").ToList();
        Assert.Contains("4500k", args);
        Assert.Contains("2500k", args);
    }

    [Fact]
    public void TwoRenditions_StreamMapContainsBothNames()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(TwoRenditions(), "input.mp4", "/out").ToList();
        var mapIdx = args.IndexOf("-var_stream_map");
        Assert.True(mapIdx >= 0);
        Assert.Contains("1080p", args[mapIdx + 1]);
        Assert.Contains("720p",  args[mapIdx + 1]);
    }

    [Fact]
    public void Args_ContainHlsFormat()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "input.mp4", "/out").ToList();
        var idx = args.IndexOf("-f");
        Assert.True(idx >= 0);
        Assert.Equal("hls", args[idx + 1]);
    }

    [Fact]
    public void Args_HlsTimeSixSeconds()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "input.mp4", "/out").ToList();
        var idx = args.IndexOf("-hls_time");
        Assert.True(idx >= 0);
        Assert.Equal("6", args[idx + 1]);
    }

    [Fact]
    public void Args_MasterPlaylistNameSet()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "input.mp4", "/out").ToList();
        var idx = args.IndexOf("-master_pl_name");
        Assert.True(idx >= 0);
        Assert.Equal("master.m3u8", args[idx + 1]);
    }

    [Fact]
    public void Args_AudioCodecIsAac()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "input.mp4", "/out").ToList();
        var idx = args.IndexOf("-c:a");
        Assert.True(idx >= 0);
        Assert.Equal("aac", args[idx + 1]);
    }

    [Fact]
    public void ToCommandLine_StartsWithFfmpeg()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "input.mp4", "/out");
        var cmd = FfmpegCommandBuilder.ToCommandLine(args);
        Assert.StartsWith("ffmpeg ", cmd);
    }

    [Fact]
    public void ToCommandLine_QuotesArgsWithSpaces()
    {
        var args = FfmpegCommandBuilder.BuildHlsArgs(OneRendition(), "my input.mp4", "/out");
        var cmd = FfmpegCommandBuilder.ToCommandLine(args);
        Assert.Contains("\"my input.mp4\"", cmd);
    }
}
