namespace WrapPassword.Application.Models;

public sealed record ArchiveBuildResult(
    string ArchivePath,
    long SizeInBytes,
    string Sha256,
    IReadOnlyList<ArchiveEntryResult> Entries);
