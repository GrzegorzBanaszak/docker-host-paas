namespace Dockerizer.Domain.Entities;

public sealed class ImageArtifact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobImageId { get; set; }
    public JobImage JobImage { get; set; } = null!;
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
