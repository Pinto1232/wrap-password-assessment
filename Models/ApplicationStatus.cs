namespace WrapPasswordAssessment.Models;

public sealed record ApplicationStatus(
    string Name,
    string Status,
    DateTimeOffset Timestamp);
