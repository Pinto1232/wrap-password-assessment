using System.IO.Compression;
using WrapPassword.Domain.Submissions;

namespace WrapPassword.Infrastructure.Packaging;

internal static class VerifiedZipArchiveWriter
{
    private static readonly DateTimeOffset StableEntryTimestamp =
        new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static async Task<VerifiedArchive> WriteAsync(
        string archivePath,
        PreparedArchiveEntry[] entries,
        CancellationToken cancellationToken)
    {
        await CreateAsync(archivePath, entries, cancellationToken);

        var archiveSize = new FileInfo(archivePath).Length;
        SubmissionArchiveRules.EnsureSizeIsAllowed(archiveSize);

        await VerifyAsync(archivePath, entries, cancellationToken);
        var archiveSha256 = await Sha256Calculator.CalculateFileAsync(
            archivePath,
            cancellationToken);

        return new VerifiedArchive(archiveSize, archiveSha256);
    }

    private static async Task CreateAsync(
        string archivePath,
        PreparedArchiveEntry[] entries,
        CancellationToken cancellationToken)
    {
        await using var archiveStream = new FileStream(
            archivePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81_920,
            useAsync: true);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true);

        foreach (var preparedEntry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var archiveEntry = archive.CreateEntry(
                preparedEntry.ArchivePath,
                CompressionLevel.Optimal);
            archiveEntry.LastWriteTime = StableEntryTimestamp;

            await using var entryStream = archiveEntry.Open();
            await entryStream.WriteAsync(preparedEntry.Content, cancellationToken);
        }
    }

    private static async Task VerifyAsync(
        string archivePath,
        PreparedArchiveEntry[] expectedEntries,
        CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var actualEntries = archive.Entries.ToDictionary(
            entry => entry.FullName,
            StringComparer.Ordinal);

        if (actualEntries.Count != expectedEntries.Length)
        {
            throw new InvalidDataException("The ZIP entry count did not pass verification.");
        }

        foreach (var expectedEntry in expectedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!actualEntries.TryGetValue(expectedEntry.ArchivePath, out var actualEntry))
            {
                throw new InvalidDataException(
                    $"The ZIP is missing required entry '{expectedEntry.ArchivePath}'.");
            }

            ArchivePathValidator.EnsureSafe(actualEntry.FullName);
            EnsureEntrySizeMatches(expectedEntry, actualEntry);

            await using var entryStream = actualEntry.Open();
            var actualSha256 = await Sha256Calculator.CalculateStreamAsync(
                entryStream,
                cancellationToken);

            if (!string.Equals(actualSha256, expectedEntry.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"ZIP entry '{expectedEntry.ArchivePath}' failed hash verification.");
            }
        }
    }

    private static void EnsureEntrySizeMatches(
        PreparedArchiveEntry expectedEntry,
        ZipArchiveEntry actualEntry)
    {
        if (actualEntry.Length != expectedEntry.Content.LongLength)
        {
            throw new InvalidDataException(
                $"ZIP entry '{expectedEntry.ArchivePath}' has an unexpected size.");
        }
    }
}

internal sealed record VerifiedArchive(long SizeInBytes, string Sha256);
