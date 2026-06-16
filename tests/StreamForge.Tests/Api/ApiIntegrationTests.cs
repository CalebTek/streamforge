using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace StreamForge.Tests.Api;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Type"]             = "Local",
                ["Storage:Local:RootPath"]   = Path.Combine(Path.GetTempPath(), $"sf-test-{Guid.NewGuid()}"),
                ["Storage:Local:BaseUrl"]    = "http://localhost/files",
                ["JobStore:Type"]            = "InMemory",
                ["Queue:Enabled"]            = "false",
                ["Auth:ApiKey"]              = ""
            });
        });
    }
}

public sealed class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(ApiFactory factory)
        => _client = factory.CreateClient();

    // ── Health ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Returns200AndOkStatus()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    // ── POST /api/jobs ────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitJob_ValidRequest_Returns202WithJobId()
    {
        var payload = new
        {
            sourceUrl = "https://example.com/video.mp4",
            outputs = new[]
            {
                new { name = "720p", width = 1280, height = 720, videoBitrateKbps = 2500 }
            }
        };

        var response = await PostJobAsync(payload);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("jobId", out var jobId));
        Assert.NotEqual(Guid.Empty, Guid.Parse(jobId.GetString()!));
        Assert.Equal("Queued", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task SubmitJob_LocationHeaderPointsToStatusEndpoint()
    {
        var payload = new
        {
            sourceUrl = "https://example.com/video.mp4",
            outputs = new[] { new { name = "480p", width = 854, height = 480, videoBitrateKbps = 1200 } }
        };

        var response = await PostJobAsync(payload);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("/api/jobs/", location);
    }

    [Fact]
    public async Task SubmitJob_MissingSourceUrl_Returns400()
    {
        var payload = new
        {
            sourceUrl = "",
            outputs = new[] { new { name = "720p", width = 1280, height = 720, videoBitrateKbps = 2500 } }
        };

        var response = await PostJobAsync(payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitJob_EmptyOutputs_Returns400()
    {
        var payload = new { sourceUrl = "https://example.com/video.mp4", outputs = Array.Empty<object>() };
        var response = await PostJobAsync(payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitJob_WithCallbackUrl_Returns202()
    {
        var payload = new
        {
            sourceUrl   = "https://example.com/video.mp4",
            outputs     = new[] { new { name = "720p", width = 1280, height = 720, videoBitrateKbps = 2500 } },
            callbackUrl = "https://client.example.com/callback"
        };

        var response = await PostJobAsync(payload);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    // ── GET /api/jobs/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task GetJob_KnownId_Returns200WithQueuedStatus()
    {
        var jobId = await SubmitAndGetIdAsync();

        var response = await _client.GetAsync($"/api/jobs/{jobId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(jobId.ToString(), body.GetProperty("jobId").GetString());
        Assert.Equal("Queued", body.GetProperty("status").GetString());
        Assert.Equal(0.0, body.GetProperty("progress").GetDouble());
    }

    [Fact]
    public async Task GetJob_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/jobs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetJob_InvalidGuid_Returns404OrBadRequest()
    {
        var response = await _client.GetAsync("/api/jobs/not-a-guid");
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> PostJobAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return _client.PostAsync("/api/jobs", content);
    }

    private async Task<Guid> SubmitAndGetIdAsync()
    {
        var payload = new
        {
            sourceUrl = "https://example.com/video.mp4",
            outputs = new[] { new { name = "720p", width = 1280, height = 720, videoBitrateKbps = 2500 } }
        };
        var response = await PostJobAsync(payload);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("jobId").GetString()!);
    }
}
