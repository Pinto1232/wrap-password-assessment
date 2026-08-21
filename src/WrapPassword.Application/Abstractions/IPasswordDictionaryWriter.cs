namespace WrapPassword.Application.Abstractions;

public interface IPasswordDictionaryWriter
{
    Task<string> WriteAsync(
        IEnumerable<string> candidates,
        string outputPath,
        CancellationToken cancellationToken = default);
}
