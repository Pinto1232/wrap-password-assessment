namespace WrapPassword.Application.Abstractions;

public interface IRecruitmentAuthenticationClient
{
    Task<Uri?> TryAuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
