using MassTransit;
using Microsoft.Extensions.Hosting;
using StreamForge.Infrastructure.Extensions;
using StreamForge.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        services.AddStreamForgeStorage(config);
        services.AddStreamForgeJobStore(config);
        services.AddWebhookNotifier();

        var ffmpegPath  = config["Ffmpeg:Path"]      ?? "ffmpeg";
        var ffprobePath = config["Ffmpeg:ProbePath"] ?? "ffprobe";

        services.AddSingleton(sp => new FfmpegRunner(
            sp.GetRequiredService<ILogger<FfmpegRunner>>(),
            ffmpegPath, ffprobePath));

        services.AddScoped<EncodeWorker>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<EncodeJobConsumer>();
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(new Uri(config["Queue:Host"] ?? "rabbitmq://localhost"), h =>
                {
                    h.Username(config["Queue:Username"] ?? "guest");
                    h.Password(config["Queue:Password"] ?? "guest");
                });
                cfg.ReceiveEndpoint("streamforge-encode", e =>
                    e.ConfigureConsumer<EncodeJobConsumer>(ctx));
            });
        });
    })
    .Build();

await host.RunAsync();
