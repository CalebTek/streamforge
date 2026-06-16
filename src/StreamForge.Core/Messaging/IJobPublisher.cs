using StreamForge.Core.Models;

namespace StreamForge.Core.Messaging;

public interface IJobPublisher
{
    Task PublishAsync(EncodeJobMessage message, CancellationToken ct = default);
}
