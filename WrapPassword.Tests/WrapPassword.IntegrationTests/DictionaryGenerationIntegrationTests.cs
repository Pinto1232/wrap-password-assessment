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
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"WrapPassword.IntegrationTests-{Guid.NewGuid():N}");
        var outputPath = Path.Combine(testDirectory, "dict.txt");

        try
        {
            var useCase = new GeneratePasswordDictionary(
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
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }
}
