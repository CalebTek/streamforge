using StreamForge.Core.Models;
using StreamForge.Core.Storage;
using StreamForge.Infrastructure.Notifications;

namespace StreamForge.Worker;

public sealed class EncodeWorker
{
    private readonly IObjectStorage _storage;
    private readonly IJobStore _jobs;
    private readonly FfmpegRunner _runner;
    private readonly WebhookNotifier _webhook;
    private readonly ILogger<EncodeWorker> _logger;

    public EncodeWorker(IObjectStorage storage, IJobStore jobs, FfmpegRunner runner,
        WebhookNotifier webhook, ILogger<EncodeWorker> logger)
    {
        _storage = storage;
        _jobs = jobs;
        _runner = runner;
        _webhook = webhook;
        _logger = logger;
    }

    public async Task ProcessAsync(EncodeJobMessage message, CancellationToken ct)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "streamforge", message.JobId.ToString());
        var sourceDir = Path.Combine(workDir, "src");
        var outputDir = Path.Combine(workDir, "out");
        Directory.CreateDirectory(sourceDir);

        _logger.LogInformation("Starting job {JobId}", message.JobId);

        try
        {
            await _jobs.UpdateStatusAsync(message.JobId, JobStatus.Running, ct: ct);

            var localInput = await _storage.DownloadAsync(message.Request.SourceUrl, sourceDir, ct);

            var exitCode = await _runner.RunAsync(
                message.Request, localInput, outputDir,
                fraction => _jobs.UpdateProgressAsync(message.JobId, fraction, ct),
                ct);

            if (exitCode != 0)
            {
                var err = $"FFmpeg exited with code {exitCode}";
                await _jobs.UpdateStatusAsync(message.JobId, JobStatus.Failed, error: err, ct: ct);
                await NotifyCallbackAsync(message, JobStatus.Failed, null, err, ct);
                return;
            }

            var manifestUrl = await _storage.UploadDirectoryAsync(
                outputDir, $"jobs/{message.JobId}", ct);

            await _jobs.UpdateStatusAsync(message.JobId, JobStatus.Completed,
                manifestUrl: manifestUrl, ct: ct);

            _logger.LogInformation("Job {JobId} completed. Manifest: {Manifest}",
                message.JobId, manifestUrl);

            await NotifyCallbackAsync(message, JobStatus.Completed, manifestUrl, null, ct);
        }
        catch (Exception ex)
        {
            await _jobs.UpdateStatusAsync(message.JobId, JobStatus.Failed, error: ex.Message, ct: ct);
            await NotifyCallbackAsync(message, JobStatus.Failed, null, ex.Message, ct);
            _logger.LogError(ex, "Job {JobId} failed", message.JobId);
            throw;
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { }
        }
    }

    private Task NotifyCallbackAsync(EncodeJobMessage message, JobStatus status,
        string? manifestUrl, string? error, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(message.Request.CallbackUrl))
            return Task.CompletedTask;

        return _webhook.NotifyAsync(
            message.JobId, status, manifestUrl, error,
            message.Request.CallbackUrl, ct);
    }
}
