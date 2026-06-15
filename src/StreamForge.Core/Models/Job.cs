namespace StreamForge.Core.Models;

public enum JobStatus { Queued, Running, Completed, Failed }

public sealed record OutputRendition
{
    public required string Name { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int VideoBitrateKbps { get; init; }
    public int AudioBitrateKbps { get; init; } = 128;
}

public sealed record JobRequest
{
    public required string SourceUrl { get; init; }
    public required IReadOnlyList<OutputRendition> Outputs { get; init; }
    public string? CallbackUrl { get; init; }
}

public sealed class EncodingJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required JobRequest Request { get; init; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public double Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ManifestUrl { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed record EncodeJobMessage
{
    public required Guid JobId { get; init; }
    public required JobRequest Request { get; init; }
}
