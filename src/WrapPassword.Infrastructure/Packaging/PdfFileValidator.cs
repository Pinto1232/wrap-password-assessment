namespace WrapPassword.Infrastructure.Packaging;

internal static class PdfFileValidator
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    public static async Task ValidateAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The CV must use the .pdf file extension.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The CV PDF could not be found.", filePath);
        }

        if (File.GetAttributes(filePath).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("The CV PDF cannot be a symbolic link.");
        }

        var fileLength = new FileInfo(filePath).Length;

        if (fileLength < PdfSignature.Length)
        {
            throw new InvalidDataException("The CV does not contain a valid PDF signature.");
        }

        var actualSignature = new byte[PdfSignature.Length];

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4_096,
            useAsync: true);

        await stream.ReadExactlyAsync(actualSignature, cancellationToken);

        if (!actualSignature.SequenceEqual(PdfSignature))
        {
            throw new InvalidDataException("The CV does not contain a valid PDF signature.");
        }
    }
}
