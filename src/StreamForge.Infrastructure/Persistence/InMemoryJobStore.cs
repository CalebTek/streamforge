using System.Collections.Concurrent;
using StreamForge.Core.Models;
using StreamForge.Core.Storage;

namespace StreamForge.Infrastructure.Persistence;

public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<Guid, EncodingJob> _jobs = new();

    public Task SaveAsync(EncodingJob job, CancellationToken ct = default)
    {
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<EncodingJob?> GetAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_jobs.GetValueOrDefault(id));

    public Task UpdateProgressAsync(Guid id, double fraction, CancellationToken ct = default)
    {
        if (_jobs.TryGetValue(id, out var job))
            job.Progress = fraction;
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(Guid id, JobStatus status, string? error = null,
        string? manifestUrl = null, CancellationToken ct = default)
    {
        if (_jobs.TryGetValue(id, out var job))
        {
            job.Status = status;
            job.ErrorMessage = error;
            if (manifestUrl is not null) job.ManifestUrl = manifestUrl;
            if (status is JobStatus.Completed or JobStatus.Failed)
                job.CompletedAt = DateTimeOffset.UtcNow;
        }
        return Task.CompletedTask;
    }
}
