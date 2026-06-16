using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamForge.Core.Models;

namespace StreamForge.Infrastructure.Notifications;

public sealed class WebhookNotifier
{
    private readonly HttpClient _http;
    private readonly ILogger<WebhookNotifier> _logger;

    public WebhookNotifier(HttpClient http, ILogger<WebhookNotifier> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task NotifyAsync(Guid jobId, JobStatus status, string? manifestUrl,
        string? error, string callbackUrl, CancellationToken ct = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                jobId,
                status = status.ToString(),
                manifestUrl,
                error
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(callbackUrl, content, ct);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Webhook for job {JobId} returned {Status}", jobId, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook notification for job {JobId} failed", jobId);
        }
    }
}
