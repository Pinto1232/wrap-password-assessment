using System.Text;
using WrapPassword.Application.Abstractions;

namespace WrapPassword.Infrastructure.Files;

public sealed class PasswordDictionaryFileWriter : IPasswordDictionaryWriter
{
    public async Task<string> WriteAsync(
        IEnumerable<string> candidates,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath)
            ?? throw new InvalidOperationException("The output directory could not be resolved.");

        Directory.CreateDirectory(outputDirectory);

        await using var writer = new StreamWriter(
            fullOutputPath,
            append: false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(candidate.AsMemory(), cancellationToken);
        }

        return fullOutputPath;
    }
}
