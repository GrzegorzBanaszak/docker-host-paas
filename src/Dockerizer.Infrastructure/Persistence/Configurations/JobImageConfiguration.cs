using Dockerizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dockerizer.Infrastructure.Persistence.Configurations;

public sealed class JobImageConfiguration : IEntityTypeConfiguration<JobImage>
{
    public void Configure(EntityTypeBuilder<JobImage> builder)
    {
        builder.ToTable("job_images");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(image => image.DetectedStack)
            .HasMaxLength(128);

        builder.Property(image => image.ImageTag)
            .HasMaxLength(256);

        builder.Property(image => image.ImageId)
            .HasMaxLength(256);

        builder.Property(image => image.SourceCommitSha)
            .HasMaxLength(64);

        builder.Property(image => image.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(image => image.CreatedAtUtc)
            .IsRequired();

        builder.HasMany(image => image.Artifacts)
            .WithOne(artifact => artifact.JobImage)
            .HasForeignKey(artifact => artifact.JobImageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(image => image.JobId);
        builder.HasIndex(image => image.CreatedAtUtc);
        builder.HasIndex(image => new { image.JobId, image.CreatedAtUtc });
    }
}
