using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;

namespace WrapPassword.Application.UseCases;

public sealed class PrepareSubmissionArchiveUseCase
{
    private const string DictionaryFileName = "dict.txt";

    private readonly GeneratePasswordDictionaryUseCase _generateDictionary;
    private readonly ISubmissionArchiveBuilder _archiveBuilder;

    public PrepareSubmissionArchiveUseCase(
        GeneratePasswordDictionaryUseCase generateDictionary,
        ISubmissionArchiveBuilder archiveBuilder)
    {
        _generateDictionary = generateDictionary
            ?? throw new ArgumentNullException(nameof(generateDictionary));
        _archiveBuilder = archiveBuilder
            ?? throw new ArgumentNullException(nameof(archiveBuilder));
    }

    public async Task<ArchiveBuildResult> ExecuteAsync(
        string repositoryRoot,
        string cvPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(cvPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullRepositoryRoot = Path.GetFullPath(repositoryRoot);
        var dictionaryPath = Path.Combine(fullRepositoryRoot, DictionaryFileName);

        await _generateDictionary.ExecuteAsync(dictionaryPath, cancellationToken);

        var request = new SubmissionArchiveRequest(
            fullRepositoryRoot,
            cvPath,
            dictionaryPath,
            outputPath);

        return await _archiveBuilder.BuildAsync(request, cancellationToken);
    }
}
