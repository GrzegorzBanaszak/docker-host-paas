using Dockerizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dockerizer.Infrastructure.Persistence.Configurations;

public sealed class ImageArtifactConfiguration : IEntityTypeConfiguration<ImageArtifact>
{
    public void Configure(EntityTypeBuilder<ImageArtifact> builder)
    {
        builder.ToTable("image_artifacts");

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

        builder.HasIndex(artifact => artifact.JobImageId);
        builder.HasIndex(artifact => new { artifact.JobImageId, artifact.Kind, artifact.Name })
            .IsUnique();
    }
}
