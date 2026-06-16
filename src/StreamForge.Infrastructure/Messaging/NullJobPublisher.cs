using StreamForge.Core.Messaging;
using StreamForge.Core.Models;

namespace StreamForge.Infrastructure.Messaging;

public sealed class NullJobPublisher : IJobPublisher
{
    public Task PublishAsync(EncodeJobMessage message, CancellationToken ct = default)
        => Task.CompletedTask;
}
