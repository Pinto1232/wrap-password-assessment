using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;
using WrapPassword.Application.Services;

namespace WrapPassword.Application.UseCases;

public sealed class GeneratePasswordDictionaryUseCase
{
    private readonly IPasswordDictionaryGenerator _generator;
    private readonly IPasswordDictionaryWriter _writer;

    public GeneratePasswordDictionaryUseCase(
        IPasswordDictionaryGenerator generator,
        IPasswordDictionaryWriter writer)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task<DictionaryGenerationResult> ExecuteAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var candidates = PasswordDictionaryValidator.Validate(_generator.Generate());

        var fullOutputPath = await _writer.WriteAsync(
            candidates,
            outputPath,
            cancellationToken);

        return new DictionaryGenerationResult(fullOutputPath, candidates.Count);
    }
}
