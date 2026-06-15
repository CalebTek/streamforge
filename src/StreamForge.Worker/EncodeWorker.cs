using StreamForge.Core.Models;
using StreamForge.Core.Storage;

namespace StreamForge.Worker;

public sealed class EncodeWorker : BackgroundService
{
    private readonly IObjectStorage _storage;
    private readonly IJobStore _jobs;
    private readonly FfmpegRunner _runner;
    private readonly ILogger<EncodeWorker> _logger;

    public EncodeWorker(IObjectStorage storage, IJobStore jobs, FfmpegRunner runner, ILogger<EncodeWorker> logger)
    {
        _storage = storage; _jobs = jobs; _runner = runner; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EncodeWorker started. Waiting for jobs from the queue.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public async Task ProcessAsync(EncodeJobMessage message, CancellationToken ct)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "streamforge", message.JobId.ToString());
        var sourceDir = Path.Combine(workDir, "src");
        var outputDir = Path.Combine(workDir, "out");
        Directory.CreateDirectory(sourceDir);
        try
        {
            await _jobs.UpdateStatusAsync(message.JobId, JobStatus.Running, ct: ct);
            var localInput = await _storage.DownloadAsync(message.Request.SourceUrl, sourceDir, ct);
            var exitCode = await _runner.RunAsync(message.Request, localInput, outputDir,
                fraction => _jobs.UpdateProgressAsync(message.JobId, fraction, ct), ct);
            if (exitCode != 0) { await _jobs.UpdateStatusAsync(message.JobId, JobStatus.Failed, error: $"FFmpeg exited with code {exitCode}", ct: ct); return; }
            var manifestUrl = await _storage.UploadDirectoryAsync(outputDir, $"jobs/{message.JobId}", ct);
            await _jobs.UpdateStatusAsync(message.JobId, JobStatus.Completed, manifestUrl: manifestUrl, ct: ct);
            _logger.LogInformation("Job {JobId} completed. Manifest: {Manifest}", message.JobId, manifestUrl);
        }
        catch (Exception ex)
        {
            await _jobs.UpdateStatusAsync(message.JobId, JobStatus.Failed, error: ex.Message, ct: ct);
            _logger.LogError(ex, "Job {JobId} failed", message.JobId);
            throw;
        }
        finally { try { Directory.Delete(workDir, recursive: true); } catch { } }
    }
}
