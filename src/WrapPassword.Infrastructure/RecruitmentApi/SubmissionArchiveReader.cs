using System.IO.Compression;
using WrapPassword.Domain.Submissions;

namespace WrapPassword.Infrastructure.RecruitmentApi;

internal static class SubmissionArchiveReader
{
    public static async Task<byte[]> ReadAsync(
        string archivePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

        var fullPath = Path.GetFullPath(archivePath);
        EnsureZipExtension(fullPath);
        EnsureRegularFile(fullPath);

        SubmissionArchiveRules.EnsureSizeIsAllowed(new FileInfo(fullPath).Length);

        var archiveBytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        SubmissionArchiveRules.EnsureSizeIsAllowed(archiveBytes.LongLength);
        EnsureReadableZip(archiveBytes);

        return archiveBytes;
    }

    private static void EnsureZipExtension(string archivePath)
    {
        if (!string.Equals(Path.GetExtension(archivePath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The submission archive must use the .zip extension.");
        }
    }

    private static void EnsureRegularFile(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException("The submission archive could not be found.");
        }

        if ((File.GetAttributes(archivePath) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                "The submission archive cannot be a symbolic link.");
        }
    }

    private static void EnsureReadableZip(byte[] archiveBytes)
    {
        try
        {
            using var stream = new MemoryStream(archiveBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            if (archive.Entries.Count == 0)
            {
                throw new InvalidDataException("The submission archive cannot be empty.");
            }
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidDataException(
                "The submission archive is not a readable ZIP file.",
                exception);
        }
    }
}
