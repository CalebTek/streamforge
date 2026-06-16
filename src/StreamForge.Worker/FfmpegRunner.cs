using System.Diagnostics;
using System.Text;
using StreamForge.Core.Encoding;
using StreamForge.Core.Models;

namespace StreamForge.Worker;

public sealed class FfmpegRunner
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;
    private readonly ILogger<FfmpegRunner> _logger;

    public FfmpegRunner(ILogger<FfmpegRunner> logger,
        string ffmpegPath = "ffmpeg", string ffprobePath = "ffprobe")
    {
        _logger = logger;
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

        _logger.LogInformation("FFmpeg command: {Command}", FfmpegCommandBuilder.ToCommandLine(args));

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

        var stderr = new StringBuilder();

        var readStderr = Task.Run(async () =>
        {
            string? line;
            while ((line = await process.StandardError.ReadLineAsync()) is not null)
                stderr.AppendLine(line);
        });

        var readProgress = Task.Run(async () =>
        {
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync()) is not null)
                await onProgress(parser.Feed(line));
        });

        await process.WaitForExitAsync(ct);
        await Task.WhenAll(readStderr, readProgress);

        _logger.LogInformation("FFmpeg exited with code {Code} for input {Input}",
            process.ExitCode, inputPath);

        if (process.ExitCode != 0)
            _logger.LogError("FFmpeg stderr:\n{Stderr}", stderr.ToString());

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
        foreach (var a in new[]
            { "-v", "quiet", "-show_entries", "format=duration",
              "-of", "default=noprint_wrappers=1:nokey=1", inputPath })
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi)!;
        var output = await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);

        return double.TryParse(output.Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var s) && s > 0
            ? s
            : throw new InvalidOperationException("Could not probe source duration.");
    }
}
