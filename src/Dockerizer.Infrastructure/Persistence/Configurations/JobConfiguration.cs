using Dockerizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dockerizer.Infrastructure.Persistence.Configurations;

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(job => job.Id);

        builder.Property(job => job.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(job => job.RepositoryUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(job => job.Branch)
            .HasMaxLength(255);

        builder.Property(job => job.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(job => job.DetectedStack)
            .HasMaxLength(128);

        builder.Property(job => job.GeneratedImageTag)
            .HasMaxLength(256);

        builder.Property(job => job.ImageId)
            .HasMaxLength(256);

        builder.Property(job => job.ContainerId)
            .HasMaxLength(128);

        builder.Property(job => job.ContainerName)
            .HasMaxLength(128);

        builder.Property(job => job.DeploymentUrl)
            .HasMaxLength(512);

        builder.Property(job => job.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(job => job.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(job => job.CurrentImage)
            .WithMany()
            .HasForeignKey(job => job.CurrentImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(job => job.Artifacts)
            .WithOne(artifact => artifact.Job)
            .HasForeignKey(artifact => artifact.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(job => job.Images)
            .WithOne(image => image.Job)
            .HasForeignKey(image => image.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(job => job.CreatedAtUtc);
        builder.HasIndex(job => job.CurrentImageId);
        builder.HasIndex(job => job.Status);
    }
}
