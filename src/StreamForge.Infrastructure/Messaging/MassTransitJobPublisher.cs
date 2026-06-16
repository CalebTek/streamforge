using MassTransit;
using StreamForge.Core.Messaging;
using StreamForge.Core.Models;

namespace StreamForge.Infrastructure.Messaging;

public sealed class MassTransitJobPublisher : IJobPublisher
{
    private readonly IPublishEndpoint _publish;

    public MassTransitJobPublisher(IPublishEndpoint publish) => _publish = publish;

    public Task PublishAsync(EncodeJobMessage message, CancellationToken ct = default)
        => _publish.Publish(message, ct);
}
