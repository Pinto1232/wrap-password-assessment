using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using WrapPassword.Application.Models;
using WrapPassword.Domain.Submissions;
using WrapPassword.Infrastructure.Packaging;
using Xunit;

namespace WrapPassword.IntegrationTests;

public sealed class SubmissionArchiveBuilderTests
{
    [Fact]
    public async Task BuildAsync_CreatesVerifiedAllowlistedArchiveAndManifest()
    {
        using var repository = await TemporarySubmissionRepository.CreateAsync();
        var outputPath = repository.GetPath("artifacts/submission.zip");
        var builder = new SubmissionArchiveBuilder();

        var result = await builder.BuildAsync(CreateRequest(repository, outputPath));

        Assert.Equal(Path.GetFullPath(outputPath), result.ArchivePath);
        Assert.True(result.SizeInBytes < SubmissionArchiveRules.MaximumSizeInBytes);
        Assert.Equal(await CalculateFileSha256Async(outputPath), result.Sha256);

        using var archive = ZipFile.OpenRead(outputPath);
        var entryNames = archive.Entries.Select(entry => entry.FullName).ToArray();

        Assert.Contains("CV/candidate-cv.pdf", entryNames);
        Assert.Contains(".env.example", entryNames);
        Assert.Contains("dict.txt", entryNames);
        Assert.Contains("README.md", entryNames);
        Assert.Contains("docs/IMPLEMENTATION_PLAN.md", entryNames);
        Assert.Contains("docs/AI_ASSISTANCE.md", entryNames);
        Assert.Contains("src/Example/Example.cs", entryNames);
        Assert.Contains("WrapPassword.Tests/ExampleTests/ExampleTests.cs", entryNames);
        Assert.Contains("submission-manifest.json", entryNames);
        Assert.DoesNotContain(entryNames, path => path.Contains("/bin/", StringComparison.Ordinal));
        Assert.DoesNotContain(entryNames, path => path.StartsWith("Database/", StringComparison.Ordinal));
        Assert.DoesNotContain(entryNames, path => path.StartsWith("artifacts/", StringComparison.Ordinal));
        Assert.DoesNotContain(entryNames, path => path.StartsWith(".git/", StringComparison.Ordinal));
        Assert.DoesNotContain(".env", entryNames);
        Assert.All(entryNames, AssertSafeArchivePath);

        var manifestEntry = archive.GetEntry("submission-manifest.json");
        Assert.NotNull(manifestEntry);
        await using var manifestStream = manifestEntry.Open();
        using var manifest = await JsonDocument.ParseAsync(manifestStream);
        var manifestFiles = manifest.RootElement.GetProperty("files");
        var readmeManifestEntry = manifestFiles
            .EnumerateArray()
            .Single(entry => entry.GetProperty("path").GetString() == "README.md");
        var readmeBytes = await File.ReadAllBytesAsync(repository.GetPath("README.md"));

        Assert.Equal(readmeBytes.LongLength, readmeManifestEntry.GetProperty("sizeInBytes").GetInt64());
        Assert.Equal(
            CalculateSha256(readmeBytes),
            readmeManifestEntry.GetProperty("sha256").GetString());
        Assert.Equal(result.Entries.Count - 1, manifestFiles.GetArrayLength());
    }

    [Fact]
    public async Task BuildAsync_ProducesDeterministicArchive()
    {
        using var repository = await TemporarySubmissionRepository.CreateAsync();
        var builder = new SubmissionArchiveBuilder();
        var firstOutputPath = repository.GetPath("artifacts/first.zip");
        var secondOutputPath = repository.GetPath("artifacts/second.zip");

        var firstResult = await builder.BuildAsync(CreateRequest(repository, firstOutputPath));
        var secondResult = await builder.BuildAsync(CreateRequest(repository, secondOutputPath));

        Assert.Equal(firstResult.Sha256, secondResult.Sha256);
        Assert.Equal(
            await File.ReadAllBytesAsync(firstOutputPath),
            await File.ReadAllBytesAsync(secondOutputPath));
    }

    [Theory]
    [InlineData("candidate-cv.txt", "%PDF-1.4\n")]
    [InlineData("candidate-cv.pdf", "not a PDF")]
    public async Task BuildAsync_RejectsInvalidCv(string fileName, string content)
    {
        using var repository = await TemporarySubmissionRepository.CreateAsync();
        var cvPath = repository.GetPath(fileName);
        var outputPath = repository.GetPath("artifacts/submission.zip");
        await File.WriteAllTextAsync(cvPath, content);
        var builder = new SubmissionArchiveBuilder();
        var request = new SubmissionArchiveRequest(
            repository.RootPath,
            cvPath,
            repository.DictionaryPath,
            outputPath);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => builder.BuildAsync(request));

        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public async Task BuildAsync_WhenCancelled_DoesNotCreateArchive()
    {
        using var repository = await TemporarySubmissionRepository.CreateAsync();
        var outputPath = repository.GetPath("artifacts/submission.zip");
        var builder = new SubmissionArchiveBuilder();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => builder.BuildAsync(
                CreateRequest(repository, outputPath),
                cancellationSource.Token));

        Assert.False(File.Exists(outputPath));
    }

    private static SubmissionArchiveRequest CreateRequest(
        TemporarySubmissionRepository repository,
        string outputPath)
    {
        return new SubmissionArchiveRequest(
            repository.RootPath,
            repository.CvPath,
            repository.DictionaryPath,
            outputPath);
    }

    private static void AssertSafeArchivePath(string path)
    {
        Assert.False(path.StartsWith('/'));
        Assert.DoesNotContain('\\', path);
        Assert.DoesNotContain("../", path, StringComparison.Ordinal);
    }

    private static string CalculateSha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    private static async Task<string> CalculateFileSha256Async(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
