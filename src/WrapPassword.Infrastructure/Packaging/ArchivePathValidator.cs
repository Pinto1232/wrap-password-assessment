namespace WrapPassword.Infrastructure.Packaging;

internal static class ArchivePathValidator
{
    public static string Normalize(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/');
    }

    public static void EnsureSafe(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath)
            || archivePath.StartsWith('/')
            || archivePath.Contains('\\')
            || archivePath.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new InvalidDataException("The archive contains an unsafe entry path.");
        }
    }
}
