using System.Diagnostics;
using StreamForge.Core.Encoding;
using StreamForge.Core.Models;

namespace StreamForge.Worker;

public sealed class FfmpegRunner
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public FfmpegRunner(string ffmpegPath = "ffmpeg", string ffprobePath = "ffprobe")
    {
        _ffmpegPath = ffmpegPath;
        _ffprobePath = ffprobePath;
    }

    public async Task<int> RunAsync(JobRequest request, string inputPath, string outputDir,
        Func<double, Task> onProgress, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        var durationSeconds = await ProbeDurationAsync(inputPath, ct);
        var parser = new FfmpegProgressParser(durationSeconds);
        var args = FfmpegCommandBuilder.BuildHlsArgs(request, inputPath, outputDir);

        var psi = new ProcessStartInfo(_ffmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var readProgress = Task.Run(async () =>
        {
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync(ct)) is not null)
                await onProgress(parser.Feed(line));
        }, ct);

        await process.WaitForExitAsync(ct);
        await readProgress;
        if (process.ExitCode == 0) await onProgress(1.0);
        return process.ExitCode;
    }

    private async Task<double> ProbeDurationAsync(string inputPath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(_ffprobePath)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in new[] { "-v", "quiet", "-show_entries", "format=duration",
            "-of", "default=noprint_wrappers=1:nokey=1", inputPath })
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi)!;
        var output = await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        return double.TryParse(output.Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var s) && s > 0
            ? s : throw new InvalidOperationException("Could not probe source duration.");
    }
}
