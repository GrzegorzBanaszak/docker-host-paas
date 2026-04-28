using Dockerizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dockerizer.Infrastructure.Persistence.Configurations;

public sealed class JobArtifactConfiguration : IEntityTypeConfiguration<JobArtifact>
{
    public void Configure(EntityTypeBuilder<JobArtifact> builder)
    {
        builder.ToTable("job_artifacts");

        builder.HasKey(artifact => artifact.Id);

        builder.Property(artifact => artifact.Kind)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(artifact => artifact.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(artifact => artifact.Content)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(artifact => artifact.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(artifact => artifact.JobId);
        builder.HasIndex(artifact => new { artifact.JobId, artifact.Kind, artifact.Name })
            .IsUnique();
    }
}
