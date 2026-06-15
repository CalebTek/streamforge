using System.Collections.Concurrent;
using StreamForge.Core.Models;
using StreamForge.Core.Storage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();
var app = builder.Build();

app.MapPost("/api/jobs", async (JobRequest request, IJobStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.SourceUrl) || request.Outputs.Count == 0)
        return Results.BadRequest("sourceUrl and at least one output are required.");
    var job = new EncodingJob { Request = request };
    await store.SaveAsync(job);
    return Results.Accepted($"/api/jobs/{job.Id}", new { jobId = job.Id, status = job.Status.ToString() });
});

app.MapGet("/api/jobs/{id:guid}", async (Guid id, IJobStore store) =>
{
    var job = await store.GetAsync(id);
    return job is null ? Results.NotFound() : Results.Ok(new
    {
        jobId = job.Id,
        status = job.Status.ToString(),
        progress = job.Progress,
        manifestUrl = job.ManifestUrl,
        error = job.ErrorMessage
    });
});

app.Run();

internal sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<Guid, EncodingJob> _jobs = new();
    public Task SaveAsync(EncodingJob job, CancellationToken ct = default) { _jobs[job.Id] = job; return Task.CompletedTask; }
    public Task<EncodingJob?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_jobs.GetValueOrDefault(id));
    public Task UpdateProgressAsync(Guid id, double fraction, CancellationToken ct = default) { if (_jobs.TryGetValue(id, out var job)) job.Progress = fraction; return Task.CompletedTask; }
    public Task UpdateStatusAsync(Guid id, JobStatus status, string? error = null, string? manifestUrl = null, CancellationToken ct = default)
    {
        if (_jobs.TryGetValue(id, out var job)) { job.Status = status; job.ErrorMessage = error; if (manifestUrl is not null) job.ManifestUrl = manifestUrl; if (status is JobStatus.Completed or JobStatus.Failed) job.CompletedAt = DateTimeOffset.UtcNow; }
        return Task.CompletedTask;
    }
}
