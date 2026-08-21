namespace WrapPassword.Application.Models;

public sealed record ArchiveEntryResult(
    string Path,
    long SizeInBytes,
    string Sha256);
