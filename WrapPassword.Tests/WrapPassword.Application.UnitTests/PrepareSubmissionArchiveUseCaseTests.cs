using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;
using WrapPassword.Application.Services;
using WrapPassword.Application.UseCases;
using WrapPassword.Domain.Passwords;
using Xunit;

namespace WrapPassword.Application.UnitTests;

public sealed class PrepareSubmissionArchiveUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_GeneratesDictionaryBeforeBuildingArchive()
    {
        var repositoryRoot = Path.Combine(Path.GetTempPath(), "submission-repository");
        var cvPath = Path.Combine(Path.GetTempPath(), "cv.pdf");
        var outputPath = Path.Combine(Path.GetTempPath(), "submission.zip");
        var dictionaryWriter = new RecordingDictionaryWriter();
        var generateDictionary = new GeneratePasswordDictionaryUseCase(
            new PasswordDictionaryGenerator(),
            dictionaryWriter);
        var archiveBuilder = new RecordingArchiveBuilder();
        var useCase = new PrepareSubmissionArchiveUseCase(
            generateDictionary,
            archiveBuilder);

        var result = await useCase.ExecuteAsync(repositoryRoot, cvPath, outputPath);

        var expectedRepositoryRoot = Path.GetFullPath(repositoryRoot);
        var expectedDictionaryPath = Path.Combine(expectedRepositoryRoot, "dict.txt");
        Assert.Equal(expectedDictionaryPath, dictionaryWriter.OutputPath);
        Assert.Equal(PasswordRules.ExpectedCandidateCount, dictionaryWriter.CandidateCount);
        Assert.NotNull(archiveBuilder.Request);
        Assert.Equal(expectedRepositoryRoot, archiveBuilder.Request.RepositoryRoot);
        Assert.Equal(expectedDictionaryPath, archiveBuilder.Request.DictionaryPath);
        Assert.Equal(cvPath, archiveBuilder.Request.CvPath);
        Assert.Equal(outputPath, archiveBuilder.Request.OutputPath);
        Assert.Same(archiveBuilder.Result, result);
    }

    private sealed class RecordingDictionaryWriter : IPasswordDictionaryWriter
    {
        public int CandidateCount { get; private set; }

        public string? OutputPath { get; private set; }

        public Task<string> WriteAsync(
            IEnumerable<string> candidates,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            CandidateCount = candidates.Count();
            OutputPath = outputPath;
            return Task.FromResult(outputPath);
        }
    }

    private sealed class RecordingArchiveBuilder : ISubmissionArchiveBuilder
    {
        public ArchiveBuildResult Result { get; } = new(
            "/tmp/submission.zip",
            1_024,
            "archive-sha256",
            []);

        public SubmissionArchiveRequest? Request { get; private set; }

        public Task<ArchiveBuildResult> BuildAsync(
            SubmissionArchiveRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(Result);
        }
    }
}
