using System.Text;
using WrapPassword.Application.Services;
using WrapPassword.Application.UseCases;
using WrapPassword.Domain.Passwords;
using WrapPassword.Infrastructure.Files;
using Xunit;

namespace WrapPassword.IntegrationTests;

public sealed class DictionaryGenerationIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_WritesCompleteUtf8DictionaryToDisk()
    {
        var testDirectory = CreateTestDirectory();
        var outputPath = Path.Combine(testDirectory, "dict.txt");

        try
        {
            var useCase = new GeneratePasswordDictionaryUseCase(
                new PasswordDictionaryGenerator(),
                new PasswordDictionaryFileWriter());

            var result = await useCase.ExecuteAsync(outputPath);
            var lines = await File.ReadAllLinesAsync(outputPath);
            var bytes = await File.ReadAllBytesAsync(outputPath);
            var utf8Preamble = Encoding.UTF8.GetPreamble();

            Assert.Equal(Path.GetFullPath(outputPath), result.OutputPath);
            Assert.Equal(PasswordRules.ExpectedCandidateCount, result.CandidateCount);
            Assert.Equal(PasswordRules.ExpectedCandidateCount, lines.Length);
            Assert.Equal(PasswordRules.ExpectedCandidateCount, lines.Distinct().Count());
            Assert.Contains("P@55w0rd", lines);
            Assert.False(bytes.Take(utf8Preamble.Length).SequenceEqual(utf8Preamble));
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [Fact]
    public async Task WriteAsync_WhenCancelled_PreservesExistingDictionary()
    {
        var testDirectory = CreateTestDirectory();
        var outputPath = Path.Combine(testDirectory, "dict.txt");
        await File.WriteAllTextAsync(outputPath, "existing-content");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        try
        {
            var writer = new PasswordDictionaryFileWriter();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => writer.WriteAsync(
                    ["replacement-content"],
                    outputPath,
                    cancellationSource.Token));

            Assert.Equal("existing-content", await File.ReadAllTextAsync(outputPath));
            Assert.Equal(outputPath, Assert.Single(Directory.GetFiles(testDirectory)));
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    private static string CreateTestDirectory()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"WrapPassword.IntegrationTests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(testDirectory);
        return testDirectory;
    }

    private static void DeleteTestDirectory(string testDirectory)
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }
}
