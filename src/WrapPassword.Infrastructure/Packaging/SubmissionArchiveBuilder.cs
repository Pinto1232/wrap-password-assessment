using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;

namespace WrapPassword.Infrastructure.Packaging;

public sealed class SubmissionArchiveBuilder : ISubmissionArchiveBuilder
{
    public async Task<ArchiveBuildResult> BuildAsync(
        SubmissionArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestValues(request);

        var repositoryRoot = GetExistingDirectory(request.RepositoryRoot);
        var cvPath = Path.GetFullPath(request.CvPath);
        var dictionaryPath = Path.GetFullPath(request.DictionaryPath);
        var outputPath = Path.GetFullPath(request.OutputPath);

        EnsurePathIsInsideRepository(repositoryRoot, dictionaryPath, "dictionary");
        EnsureZipExtension(outputPath);
        await PdfFileValidator.ValidateAsync(cvPath, cancellationToken);

        var payloadEntries = await SubmissionFileCollector.CollectAsync(
            repositoryRoot,
            cvPath,
            dictionaryPath,
            cancellationToken);
        var allEntries = SubmissionManifestFactory.AddManifest(payloadEntries);

        EnsureOutputDoesNotReplaceInput(outputPath, allEntries);

        var outputDirectory = Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException("The archive output directory could not be resolved.");

        Directory.CreateDirectory(outputDirectory);
        var temporaryArchivePath = CreateTemporaryArchivePath(outputDirectory, outputPath);

        try
        {
            var verifiedArchive = await VerifiedZipArchiveWriter.WriteAsync(
                temporaryArchivePath,
                allEntries,
                cancellationToken);

            File.Move(temporaryArchivePath, outputPath, overwrite: true);

            return CreateResult(outputPath, verifiedArchive, allEntries);
        }
        finally
        {
            DeleteTemporaryArchive(temporaryArchivePath);
        }
    }

    private static void ValidateRequestValues(SubmissionArchiveRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CvPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DictionaryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);
    }

    private static string GetExistingDirectory(string directoryPath)
    {
        var fullPath = Path.GetFullPath(directoryPath);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(
                "The repository root directory could not be found.");
        }

        return fullPath;
    }

    private static void EnsureZipExtension(string outputPath)
    {
        if (!string.Equals(Path.GetExtension(outputPath), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The archive output path must use the .zip extension.");
        }
    }

    private static void EnsurePathIsInsideRepository(
        string repositoryRoot,
        string filePath,
        string description)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath);

        if (relativePath == ".."
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException(
                $"The {description} file must be inside the repository root.");
        }
    }

    private static void EnsureOutputDoesNotReplaceInput(
        string outputPath,
        IEnumerable<PreparedArchiveEntry> entries)
    {
        var pathComparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (entries.Any(entry => entry.SourcePath is not null
            && string.Equals(entry.SourcePath, outputPath, pathComparison)))
        {
            throw new InvalidOperationException(
                "The archive output path cannot replace one of its input files.");
        }
    }

    private static string CreateTemporaryArchivePath(
        string outputDirectory,
        string outputPath)
    {
        return Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
    }

    private static ArchiveBuildResult CreateResult(
        string outputPath,
        VerifiedArchive archive,
        IEnumerable<PreparedArchiveEntry> entries)
    {
        var entryResults = entries
            .Select(entry => new ArchiveEntryResult(
                entry.ArchivePath,
                entry.Content.LongLength,
                entry.Sha256))
            .ToArray();

        return new ArchiveBuildResult(
            outputPath,
            archive.SizeInBytes,
            archive.Sha256,
            entryResults);
    }

    private static void DeleteTemporaryArchive(string temporaryArchivePath)
    {
        if (File.Exists(temporaryArchivePath))
        {
            File.Delete(temporaryArchivePath);
        }
    }
}
