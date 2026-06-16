using Microsoft.EntityFrameworkCore;
using StreamForge.Core.Models;
using StreamForge.Core.Storage;

namespace StreamForge.Infrastructure.Persistence;

public sealed class PostgresJobStore : IJobStore
{
    private readonly IDbContextFactory<StreamForgeDbContext> _factory;

    public PostgresJobStore(IDbContextFactory<StreamForgeDbContext> factory) => _factory = factory;

    public async Task SaveAsync(EncodingJob job, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
    }

    public async Task<EncodingJob?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    public async Task UpdateProgressAsync(Guid id, double fraction, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Jobs
            .Where(j => j.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Progress, fraction), ct);
    }

    public async Task UpdateStatusAsync(Guid id, JobStatus status, string? error = null,
        string? manifestUrl = null, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var completedAt = status is JobStatus.Completed or JobStatus.Failed
            ? (DateTimeOffset?)DateTimeOffset.UtcNow : null;

        await db.Jobs
            .Where(j => j.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, status)
                .SetProperty(j => j.ErrorMessage, error)
                .SetProperty(j => j.ManifestUrl, j => manifestUrl ?? j.ManifestUrl)
                .SetProperty(j => j.CompletedAt, j => completedAt ?? j.CompletedAt), ct);
    }
}
