using System.Security.Cryptography;

namespace WrapPassword.Infrastructure.Packaging;

internal static class Sha256Calculator
{
    public static string Calculate(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    public static async Task<string> CalculateFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81_920,
            useAsync: true);

        return await CalculateStreamAsync(stream, cancellationToken);
    }

    public static async Task<string> CalculateStreamAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
