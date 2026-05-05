using Dockerizer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dockerizer.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(project => project.RepositoryUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(project => project.DefaultBranch)
            .HasMaxLength(255);

        builder.Property(project => project.DefaultProjectPath)
            .HasMaxLength(1024);

        builder.Property(project => project.PublicAccessEnabled)
            .IsRequired();

        builder.Property(project => project.PublicHostname)
            .HasMaxLength(255);

        builder.Property(project => project.DeploymentUrl)
            .HasMaxLength(512);

        builder.Property(project => project.RouteStatus)
            .HasMaxLength(64);

        builder.Property(project => project.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(project => project.CurrentJob)
            .WithMany()
            .HasForeignKey(project => project.CurrentJobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(project => project.CurrentImage)
            .WithMany()
            .HasForeignKey(project => project.CurrentImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(project => project.Jobs)
            .WithOne(job => job.Project)
            .HasForeignKey(job => job.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(project => project.CreatedAtUtc);
        builder.HasIndex(project => project.ArchivedAtUtc);
        builder.HasIndex(project => project.CurrentJobId);
        builder.HasIndex(project => project.CurrentImageId);
        builder.HasIndex(project => project.PublicAccessEnabled);
        builder.HasIndex(project => project.PublicHostname);
        builder.HasIndex(project => new { project.RepositoryUrl, project.DefaultProjectPath });
    }
}
