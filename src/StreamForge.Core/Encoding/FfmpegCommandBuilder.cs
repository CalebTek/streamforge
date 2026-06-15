using StreamForge.Core.Models;

namespace StreamForge.Core.Encoding;

public static class FfmpegCommandBuilder
{
    public static IReadOnlyList<string> BuildHlsArgs(
        JobRequest request, string inputPath, string outputDir)
    {
        if (request.Outputs.Count == 0)
            throw new ArgumentException("At least one output rendition is required.");

        var args = new List<string>
        {
            "-hide_banner", "-i", inputPath,
            "-progress", "pipe:1", "-nostats"
        };

        for (var i = 0; i < request.Outputs.Count; i++)
        {
            var r = request.Outputs[i];
            args.AddRange(new[]
            {
                "-map", "0:v:0",
                $"-c:v:{i}", "libx264",
                $"-b:v:{i}", $"{r.VideoBitrateKbps}k",
                $"-vf:{i}", $"scale={r.Width}:{r.Height}",
                "-preset", "fast",
                "-x264-params", "keyint=48:min-keyint=48:scenecut=0"
            });
        }

        args.AddRange(new[] { "-map", "0:a:0", "-c:a", "aac", "-b:a", "128k" });

        var streamMap = string.Join(" ",
            Enumerable.Range(0, request.Outputs.Count)
                      .Select(i => $"v:{i},a:0,name:{request.Outputs[i].Name}"));

        args.AddRange(new[]
        {
            "-f", "hls", "-hls_time", "6", "-hls_playlist_type", "vod",
            "-hls_segment_filename", Path.Combine(outputDir, "%v", "segment_%03d.ts"),
            "-master_pl_name", "master.m3u8",
            "-var_stream_map", streamMap,
            Path.Combine(outputDir, "%v", "playlist.m3u8")
        });

        return args;
    }

    public static string ToCommandLine(IReadOnlyList<string> args) =>
        "ffmpeg " + string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
}
