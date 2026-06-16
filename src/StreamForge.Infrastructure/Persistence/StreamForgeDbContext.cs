using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreamForge.Core.Models;

namespace StreamForge.Infrastructure.Persistence;

public sealed class StreamForgeDbContext : DbContext
{
    public StreamForgeDbContext(DbContextOptions<StreamForgeDbContext> options) : base(options) { }

    public DbSet<EncodingJob> Jobs => Set<EncodingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncodingJob>(e =>
        {
            e.ToTable("encoding_jobs");
            e.HasKey(j => j.Id);
            e.Property(j => j.Id).ValueGeneratedNever();
            e.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(j => j.Request)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<JobRequest>(v, JsonSerializerOptions.Default)!);
            e.Property(j => j.ErrorMessage).HasMaxLength(2000);
            e.Property(j => j.ManifestUrl).HasMaxLength(2000);
        });
    }
}
