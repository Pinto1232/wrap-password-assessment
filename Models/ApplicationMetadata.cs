namespace WrapPasswordAssessment.Models;

public sealed class ApplicationMetadata
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
