using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;
using WrapPassword.Application.Services;

namespace WrapPassword.Application.UseCases;

public sealed class AuthenticatePasswordCandidatesUseCase
{
    private readonly IPasswordDictionaryGenerator _generator;
    private readonly IRecruitmentAuthenticationClient _authenticationClient;

    public AuthenticatePasswordCandidatesUseCase(
        IPasswordDictionaryGenerator generator,
        IRecruitmentAuthenticationClient authenticationClient)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _authenticationClient = authenticationClient
            ?? throw new ArgumentNullException(nameof(authenticationClient));
    }

    public async Task<AuthenticationResult> ExecuteAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var candidates = PasswordDictionaryValidator.Validate(_generator.Generate());

        for (var index = 0; index < candidates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uploadUri = await _authenticationClient.TryAuthenticateAsync(
                username,
                candidates[index],
                cancellationToken);

            if (uploadUri is not null)
            {
                return new AuthenticationResult(uploadUri, index + 1);
            }
        }

        throw new InvalidOperationException(
            "Authentication failed for every generated password candidate.");
    }
}
