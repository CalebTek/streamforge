using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamForge.Core.Messaging;
using StreamForge.Core.Storage;
using StreamForge.Infrastructure.Messaging;
using StreamForge.Infrastructure.Notifications;
using StreamForge.Infrastructure.Persistence;
using StreamForge.Infrastructure.Storage;

namespace StreamForge.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddStreamForgeStorage(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient();

        var type = config["Storage:Type"] ?? "Local";

        if (type.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            var s = config.GetSection("Storage:S3");
            services.AddSingleton<IObjectStorage>(sp => new S3ObjectStorage(
                sp.GetRequiredService<IHttpClientFactory>(),
                s["ServiceUrl"] ?? "",
                s["BucketName"] ?? "streamforge",
                s["Region"] ?? "us-east-1",
                s["AccessKey"] ?? "",
                s["SecretKey"] ?? "",
                s.GetValue<bool>("PathStyle"),
                s["BaseUrl"] ?? ""));
        }
        else
        {
            var l = config.GetSection("Storage:Local");
            services.AddSingleton<IObjectStorage>(sp => new LocalDiskObjectStorage(
                sp.GetRequiredService<IHttpClientFactory>(),
                l["RootPath"] ?? "output",
                l["BaseUrl"] ?? "http://localhost:5000/files"));
        }

        return services;
    }

    public static IServiceCollection AddStreamForgeJobStore(
        this IServiceCollection services, IConfiguration config)
    {
        var type = config["JobStore:Type"] ?? "InMemory";

        if (type.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var cs = config["JobStore:ConnectionString"]
                ?? throw new InvalidOperationException("JobStore:ConnectionString is required for Postgres.");

            services.AddDbContextFactory<StreamForgeDbContext>(opts =>
                opts.UseNpgsql(cs));

            // Ensure schema exists on startup.
            services.AddSingleton<PostgresJobStore>();
            services.AddSingleton<IJobStore>(sp =>
            {
                var store = sp.GetRequiredService<PostgresJobStore>();
                using var db = sp.GetRequiredService<IDbContextFactory<StreamForgeDbContext>>()
                                 .CreateDbContext();
                db.Database.EnsureCreated();
                return store;
            });
        }
        else
        {
            services.AddSingleton<IJobStore, InMemoryJobStore>();
        }

        return services;
    }

    public static IServiceCollection AddStreamForgePublisher(
        this IServiceCollection services, IConfiguration config)
    {
        var enabled = config.GetValue<bool>("Queue:Enabled");

        if (enabled)
        {
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(new Uri(config["Queue:Host"] ?? "rabbitmq://localhost"), h =>
                    {
                        h.Username(config["Queue:Username"] ?? "guest");
                        h.Password(config["Queue:Password"] ?? "guest");
                    });
                    cfg.ConfigureEndpoints(ctx);
                });
            });
            services.AddScoped<IJobPublisher, MassTransitJobPublisher>();
        }
        else
        {
            services.AddSingleton<IJobPublisher, NullJobPublisher>();
        }

        return services;
    }

    public static IServiceCollection AddWebhookNotifier(this IServiceCollection services)
    {
        services.AddHttpClient<WebhookNotifier>();
        return services;
    }
}
