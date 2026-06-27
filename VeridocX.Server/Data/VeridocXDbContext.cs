using Microsoft.EntityFrameworkCore;
using VeridocX.Server.Domain;

namespace VeridocX.Server.Data;

public class VeridocXDbContext(DbContextOptions<VeridocXDbContext> options) : DbContext(options)
{
    public DbSet<AnalysisJob> Jobs => Set<AnalysisJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var job = modelBuilder.Entity<AnalysisJob>();

        job.HasKey(j => j.Id);

        job.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        job.Property(j => j.DocumentType).HasConversion<string>().HasMaxLength(20);

        job.Property(j => j.PipelineVersion).HasMaxLength(20);

        job.Property(j => j.Subject).HasMaxLength(40);

        job.Property(j => j.Fingerprint).HasMaxLength(64);
        job.HasIndex(j => j.Fingerprint);

        job.Property(j => j.ResultJson).HasColumnType("jsonb");

        job.HasIndex(j => new { j.Status, j.CreatedAt });
    }
}
