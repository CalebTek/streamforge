using StreamForge.Core.Models;

namespace StreamForge.Core.Storage;

public interface IObjectStorage
{
    Task<string> DownloadAsync(string sourceUrl, string destinationDir, CancellationToken ct = default);
    Task<string> UploadDirectoryAsync(string localDir, string keyPrefix, CancellationToken ct = default);
}

public interface IJobStore
{
    Task SaveAsync(EncodingJob job, CancellationToken ct = default);
    Task<EncodingJob?> GetAsync(Guid id, CancellationToken ct = default);
    Task UpdateProgressAsync(Guid id, double fraction, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, JobStatus status, string? error = null, string? manifestUrl = null, CancellationToken ct = default);
}
