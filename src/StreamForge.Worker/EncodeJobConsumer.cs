using MassTransit;
using StreamForge.Core.Models;

namespace StreamForge.Worker;

public sealed class EncodeJobConsumer : IConsumer<EncodeJobMessage>
{
    private readonly EncodeWorker _worker;

    public EncodeJobConsumer(EncodeWorker worker) => _worker = worker;

    public Task Consume(ConsumeContext<EncodeJobMessage> context)
        => _worker.ProcessAsync(context.Message, context.CancellationToken);
}
