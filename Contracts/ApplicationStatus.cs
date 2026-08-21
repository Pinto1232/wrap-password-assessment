namespace WrapPassword.Contracts;

public sealed record ApplicationStatus(
    string Name,
    string Status,
    string Database,
    DateTimeOffset Timestamp);
