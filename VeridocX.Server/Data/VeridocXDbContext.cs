using Microsoft.EntityFrameworkCore;
using VeridocX.Server.Domain;
using VeridocX.Server.Domain.Affordability;

namespace VeridocX.Server.Data;

public class VeridocXDbContext(DbContextOptions<VeridocXDbContext> options) : DbContext(options)
{
    public DbSet<AnalysisJob> Jobs => Set<AnalysisJob>();

    public DbSet<AffordabilityAssessment> AffordabilityAssessments => Set<AffordabilityAssessment>();

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

        var afford = modelBuilder.Entity<AffordabilityAssessment>();

        afford.HasKey(a => a.Id);

        afford.Property(a => a.SubjectFingerprint).HasMaxLength(64);
        afford.HasIndex(a => a.SubjectFingerprint);

        afford.Property(a => a.GrossMonthlyIncome).HasPrecision(18, 2);
        afford.Property(a => a.DiscretionaryIncome).HasPrecision(18, 2);
        afford.Property(a => a.ProposedInstalment).HasPrecision(18, 2);

        afford.Property(a => a.ResultJson).HasColumnType("jsonb");

        afford.HasIndex(a => a.CreatedAt);
    }
}
