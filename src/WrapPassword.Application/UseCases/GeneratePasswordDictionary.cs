using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;
using WrapPassword.Domain.Passwords;

namespace WrapPassword.Application.UseCases;

public sealed class GeneratePasswordDictionary
{
    private readonly IPasswordDictionaryGenerator _generator;
    private readonly IPasswordDictionaryWriter _writer;

    public GeneratePasswordDictionary(
        IPasswordDictionaryGenerator generator,
        IPasswordDictionaryWriter writer)
    {
        _generator = generator;
        _writer = writer;
    }

    public async Task<DictionaryGenerationResult> ExecuteAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var candidates = _generator.Generate().ToArray();

        if (candidates.Length != PasswordRules.ExpectedCandidateCount
            || candidates.Distinct(StringComparer.Ordinal).Count() != candidates.Length)
        {
            throw new InvalidOperationException(
                "The generated password dictionary did not pass validation.");
        }

        var fullOutputPath = await _writer.WriteAsync(
            candidates,
            outputPath,
            cancellationToken);

        return new DictionaryGenerationResult(fullOutputPath, candidates.Length);
    }
}
