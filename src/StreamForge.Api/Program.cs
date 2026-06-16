using Microsoft.Extensions.FileProviders;
using StreamForge.Core.Messaging;
using StreamForge.Core.Models;
using StreamForge.Core.Storage;
using StreamForge.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStreamForgeStorage(builder.Configuration);
builder.Services.AddStreamForgeJobStore(builder.Configuration);
builder.Services.AddStreamForgePublisher(builder.Configuration);

var app = builder.Build();

// Health endpoint (FR-14)
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// API key auth (M4 hardening) — disabled when ApiKey is not configured
var apiKey = builder.Configuration["Auth:ApiKey"];
if (!string.IsNullOrWhiteSpace(apiKey))
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key) || key != apiKey)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
        }
        await next();
    });
}

// Serve local output files when using local disk storage
if ((builder.Configuration["Storage:Type"] ?? "Local")
        .Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    var rootPath = builder.Configuration["Storage:Local:RootPath"] ?? "output";
    Directory.CreateDirectory(rootPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.GetFullPath(rootPath)),
        RequestPath = "/files"
    });
}

// FR-1 to FR-5: job submission
app.MapPost("/api/jobs", async (JobRequest request, IJobStore store, IJobPublisher publisher) =>
{
    if (string.IsNullOrWhiteSpace(request.SourceUrl) || request.Outputs.Count == 0)
        return Results.BadRequest("sourceUrl and at least one output are required.");

    var job = new EncodingJob { Request = request };
    await store.SaveAsync(job);
    await publisher.PublishAsync(new EncodeJobMessage { JobId = job.Id, Request = request });

    return Results.Accepted(
        $"/api/jobs/{job.Id}",
        new { jobId = job.Id, status = job.Status.ToString() });
});

// FR-12, FR-13: status polling
app.MapGet("/api/jobs/{id:guid}", async (Guid id, IJobStore store) =>
{
    var job = await store.GetAsync(id);
    return job is null
        ? Results.NotFound()
        : Results.Ok(new
        {
            jobId = job.Id,
            status = job.Status.ToString(),
            progress = job.Progress,
            manifestUrl = job.ManifestUrl,
            error = job.ErrorMessage
        });
});

app.Run();

// Makes the implicit Program class visible to WebApplicationFactory in the test project
public partial class Program { }
