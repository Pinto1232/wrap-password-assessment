using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Services;
using WrapPassword.Application.UseCases;
using WrapPassword.Domain.Passwords;
using Xunit;

namespace WrapPassword.Application.UnitTests;

public sealed class AuthenticatePasswordCandidatesUseCaseTests
{
    private static readonly Uri UploadUri = new(
        "https://recruitment.warpdevelopment.co.za/v2/api/upload/test-token/");

    [Fact]
    public async Task ExecuteAsync_StopsAfterSuccessfulCandidate()
    {
        var generator = new PasswordDictionaryGenerator();
        var expectedPassword = generator.Generate().ElementAt(2);
        var client = new RecordingAuthenticationClient(expectedPassword);
        var useCase = new AuthenticatePasswordCandidatesUseCase(generator, client);

        var result = await useCase.ExecuteAsync("John");

        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(UploadUri, result.UploadUri);
        Assert.Equal(3, client.Attempts.Count);
        Assert.Equal(expectedPassword, client.Attempts[^1].Password);
        Assert.All(client.Attempts, attempt => Assert.Equal("John", attempt.Username));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsAfterEveryCandidateIsRejected()
    {
        var generator = new PasswordDictionaryGenerator();
        var client = new RecordingAuthenticationClient(successfulPassword: null);
        var useCase = new AuthenticatePasswordCandidatesUseCase(generator, client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync("John"));

        Assert.Equal(PasswordRules.ExpectedCandidateCount, client.Attempts.Count);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCallApiWhenDictionaryIsInvalid()
    {
        var generator = new StubDictionaryGenerator(["password"]);
        var client = new RecordingAuthenticationClient("password");
        var useCase = new AuthenticatePasswordCandidatesUseCase(generator, client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync("John"));

        Assert.Empty(client.Attempts);
    }

    [Fact]
    public async Task ExecuteAsync_HonorsCancellationBeforeFirstAttempt()
    {
        var client = new RecordingAuthenticationClient(successfulPassword: null);
        var useCase = new AuthenticatePasswordCandidatesUseCase(
            new PasswordDictionaryGenerator(),
            client);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => useCase.ExecuteAsync("John", cancellationSource.Token));

        Assert.Empty(client.Attempts);
    }

    private sealed class RecordingAuthenticationClient : IRecruitmentAuthenticationClient
    {
        private readonly string? _successfulPassword;

        public RecordingAuthenticationClient(string? successfulPassword)
        {
            _successfulPassword = successfulPassword;
        }

        public List<(string Username, string Password)> Attempts { get; } = [];

        public Task<Uri?> TryAuthenticateAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            Attempts.Add((username, password));

            var result = string.Equals(password, _successfulPassword, StringComparison.Ordinal)
                ? UploadUri
                : null;

            return Task.FromResult(result);
        }
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
}
