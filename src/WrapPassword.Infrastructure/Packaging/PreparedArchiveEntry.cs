namespace WrapPassword.Infrastructure.Packaging;

internal sealed record PreparedArchiveEntry(
    string? SourcePath,
    string ArchivePath,
    byte[] Content,
    string Sha256)
{
    public static PreparedArchiveEntry FromContent(
        string archivePath,
        byte[] content)
    {
        return new PreparedArchiveEntry(
            SourcePath: null,
            archivePath,
            content,
            Sha256Calculator.Calculate(content));
    }

    public static async Task<PreparedArchiveEntry> FromFileAsync(
        string sourcePath,
        string archivePath,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllBytesAsync(sourcePath, cancellationToken);

        return new PreparedArchiveEntry(
            Path.GetFullPath(sourcePath),
            archivePath,
            content,
            Sha256Calculator.Calculate(content));
    }
}
