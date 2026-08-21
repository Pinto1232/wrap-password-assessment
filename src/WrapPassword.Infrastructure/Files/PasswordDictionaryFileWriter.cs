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
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath)
            ?? throw new InvalidOperationException("The output directory could not be resolved.");

        Directory.CreateDirectory(outputDirectory);
        var temporaryOutputPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(fullOutputPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await WriteCandidatesAsync(candidates, temporaryOutputPath, cancellationToken);
            File.Move(temporaryOutputPath, fullOutputPath, overwrite: true);
            return fullOutputPath;
        }
        finally
        {
            if (File.Exists(temporaryOutputPath))
            {
                File.Delete(temporaryOutputPath);
            }
        }
    }

    private static async Task WriteCandidatesAsync(
        IEnumerable<string> candidates,
        string outputPath,
        CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(
            outputPath,
            append: false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(candidate.AsMemory(), cancellationToken);
        }
    }
}
