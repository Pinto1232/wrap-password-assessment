namespace WrapPassword.Infrastructure.Packaging;

internal static class SubmissionFileCollector
{
    private static readonly string[] RootFileAllowlist =
    [
        ".editorconfig",
        ".env.example",
        ".gitignore",
        "appsettings.Development.json",
        "appsettings.json",
        "Directory.Build.props",
        "Program.cs",
        "README.md",
        "WrapPassword.csproj",
        "WrapPassword.sln",
    ];

    private static readonly SourceDirectoryRule[] SourceDirectoryAllowlist =
    [
        new("Contracts", [".cs"]),
        new("Data", [".cs"]),
        new("Endpoints", [".cs"]),
        new("Properties", [".json"]),
        new("src", [".cs", ".csproj"]),
        new("WrapPassword.Tests", [".cs", ".csproj"]),
    ];

    private static readonly HashSet<string> ExcludedDirectoryNames = new(
        [
            ".git",
            ".idea",
            ".vs",
            ".vscode",
            "artifacts",
            "bin",
            "coverage",
            "Database",
            "obj",
            "TestResults",
        ],
        StringComparer.OrdinalIgnoreCase);

    public static async Task<PreparedArchiveEntry[]> CollectAsync(
        string repositoryRoot,
        string cvPath,
        string dictionaryPath,
        CancellationToken cancellationToken)
    {
        var entries = new Dictionary<string, PreparedArchiveEntry>(StringComparer.Ordinal);

        await AddRequiredFileAsync(
            entries,
            cvPath,
            $"CV/{Path.GetFileName(cvPath)}",
            cancellationToken);
        await AddRequiredFileAsync(entries, dictionaryPath, "dict.txt", cancellationToken);

        foreach (var fileName in RootFileAllowlist)
        {
            await AddRequiredFileAsync(
                entries,
                Path.Combine(repositoryRoot, fileName),
                fileName,
                cancellationToken);
        }

        await AddRequiredFileAsync(
            entries,
            Path.Combine(repositoryRoot, "docs", "IMPLEMENTATION_PLAN.md"),
            "docs/IMPLEMENTATION_PLAN.md",
            cancellationToken);
        await AddRequiredFileAsync(
            entries,
            Path.Combine(repositoryRoot, "docs", "AI_ASSISTANCE.md"),
            "docs/AI_ASSISTANCE.md",
            cancellationToken);

        foreach (var directoryRule in SourceDirectoryAllowlist)
        {
            await AddSourceDirectoryAsync(
                entries,
                repositoryRoot,
                directoryRule,
                cancellationToken);
        }

        EnsureRequiredSourceExists(entries.Keys, "src/", "application");
        EnsureRequiredSourceExists(entries.Keys, "WrapPassword.Tests/", "automated test");

        return entries.Values
            .OrderBy(entry => entry.ArchivePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static void EnsureRequiredSourceExists(
        IEnumerable<string> archivePaths,
        string pathPrefix,
        string description)
    {
        if (!archivePaths.Any(path => path.StartsWith(pathPrefix, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"The archive must contain {description} source code.");
        }
    }

    private static async Task AddSourceDirectoryAsync(
        IDictionary<string, PreparedArchiveEntry> entries,
        string repositoryRoot,
        SourceDirectoryRule rule,
        CancellationToken cancellationToken)
    {
        var sourceDirectory = Path.Combine(repositoryRoot, rule.RelativePath);

        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        foreach (var sourcePath in EnumerateSourceFiles(
                     sourceDirectory,
                     rule.AllowedExtensions,
                     cancellationToken))
        {
            var archivePath = ArchivePathValidator.Normalize(
                Path.GetRelativePath(repositoryRoot, sourcePath));

            await AddRequiredFileAsync(
                entries,
                sourcePath,
                archivePath,
                cancellationToken);
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(
        string rootDirectory,
        IReadOnlySet<string> allowedExtensions,
        CancellationToken cancellationToken)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootDirectory);

        while (pendingDirectories.TryPop(out var currentDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var filePath in Directory.EnumerateFiles(currentDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (allowedExtensions.Contains(Path.GetExtension(filePath))
                    && !IsSymbolicLink(filePath))
                {
                    yield return filePath;
                }
            }

            foreach (var directoryPath in Directory.EnumerateDirectories(currentDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var directoryName = Path.GetFileName(directoryPath);

                if (!ExcludedDirectoryNames.Contains(directoryName)
                    && !IsSymbolicLink(directoryPath))
                {
                    pendingDirectories.Push(directoryPath);
                }
            }
        }
    }

    private static async Task AddRequiredFileAsync(
        IDictionary<string, PreparedArchiveEntry> entries,
        string sourcePath,
        string archivePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                $"Required submission file '{archivePath}' could not be found.",
                sourcePath);
        }

        if (IsSymbolicLink(sourcePath))
        {
            throw new InvalidDataException(
                $"Required submission file '{archivePath}' cannot be a symbolic link.");
        }

        var normalizedArchivePath = ArchivePathValidator.Normalize(archivePath);
        ArchivePathValidator.EnsureSafe(normalizedArchivePath);

        var entry = await PreparedArchiveEntry.FromFileAsync(
            sourcePath,
            normalizedArchivePath,
            cancellationToken);

        if (!entries.TryAdd(normalizedArchivePath, entry))
        {
            throw new InvalidOperationException(
                $"The archive contains a duplicate entry path: {normalizedArchivePath}");
        }
    }

    private static bool IsSymbolicLink(string path)
    {
        return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
    }

    private sealed record SourceDirectoryRule(
        string RelativePath,
        IReadOnlySet<string> AllowedExtensions)
    {
        public SourceDirectoryRule(string relativePath, string[] allowedExtensions)
            : this(
                relativePath,
                new HashSet<string>(allowedExtensions, StringComparer.OrdinalIgnoreCase))
        {
        }
    }
}
