using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Services;
using WrapPassword.Application.UseCases;
using WrapPassword.Domain.Passwords;
using Xunit;

namespace WrapPassword.Application.UnitTests;

public sealed class GeneratePasswordDictionaryTests
{
    [Fact]
    public async Task ExecuteAsync_WritesValidatedCandidatesAndReturnsResult()
    {
        var writer = new RecordingDictionaryWriter();
        var useCase = new GeneratePasswordDictionary(
            new PasswordDictionaryGenerator(),
            writer);

        var result = await useCase.ExecuteAsync("test-dict.txt");

        Assert.True(writer.WasCalled);
        Assert.Equal(PasswordRules.ExpectedCandidateCount, writer.Candidates.Count);
        Assert.Equal(PasswordRules.ExpectedCandidateCount, result.CandidateCount);
        Assert.Equal("test-dict.txt", result.OutputPath);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotWriteWhenCandidateCountIsInvalid()
    {
        var generator = new StubDictionaryGenerator(["password"]);
        var writer = new RecordingDictionaryWriter();
        var useCase = new GeneratePasswordDictionary(generator, writer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync("test-dict.txt"));

        Assert.False(writer.WasCalled);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotWriteWhenCandidatesContainDuplicates()
    {
        var duplicateCandidates = Enumerable.Repeat(
            "password",
            PasswordRules.ExpectedCandidateCount);
        var generator = new StubDictionaryGenerator(duplicateCandidates);
        var writer = new RecordingDictionaryWriter();
        var useCase = new GeneratePasswordDictionary(generator, writer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync("test-dict.txt"));

        Assert.False(writer.WasCalled);
    }

    private sealed class StubDictionaryGenerator : IPasswordDictionaryGenerator
    {
        private readonly IEnumerable<string> _candidates;

        public StubDictionaryGenerator(IEnumerable<string> candidates)
        {
            _candidates = candidates;
        }

        public IEnumerable<string> Generate() => _candidates;
    }

    private sealed class RecordingDictionaryWriter : IPasswordDictionaryWriter
    {
        public bool WasCalled { get; private set; }

        public IReadOnlyList<string> Candidates { get; private set; } = [];

        public Task<string> WriteAsync(
            IEnumerable<string> candidates,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            Candidates = candidates.ToArray();
            return Task.FromResult(outputPath);
        }
    }
}
