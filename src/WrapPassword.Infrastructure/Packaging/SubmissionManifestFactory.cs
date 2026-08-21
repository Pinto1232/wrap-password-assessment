using System.Text.Json;

namespace WrapPassword.Infrastructure.Packaging;

internal static class SubmissionManifestFactory
{
    private const string ManifestPath = "submission-manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static PreparedArchiveEntry[] AddManifest(
        IReadOnlyList<PreparedArchiveEntry> payloadEntries)
    {
        var manifest = new ArchiveManifest(
            1,
            payloadEntries
                .Select(entry => new ArchiveManifestEntry(
                    entry.ArchivePath,
                    entry.Content.LongLength,
                    entry.Sha256))
                .ToArray());
        var manifestContent = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        var manifestEntry = PreparedArchiveEntry.FromContent(ManifestPath, manifestContent);

        return payloadEntries
            .Append(manifestEntry)
            .OrderBy(entry => entry.ArchivePath, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record ArchiveManifest(
        int SchemaVersion,
        IReadOnlyList<ArchiveManifestEntry> Files);

    private sealed record ArchiveManifestEntry(
        string Path,
        long SizeInBytes,
        string Sha256);
}
