using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;

namespace WrapPassword.Cli;

public sealed class ConsoleLiveSubmissionConfirmation : ILiveSubmissionConfirmation
{
    private const string RequiredPhrase = "SUBMIT";

    private readonly TextReader _input;
    private readonly TextWriter _output;

    public ConsoleLiveSubmissionConfirmation(TextReader input, TextWriter output)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public Task<bool> ConfirmAsync(
        ArchiveBuildResult archive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);
        cancellationToken.ThrowIfCancellationRequested();

        _output.WriteLine();
        _output.WriteLine("Submission archive is ready:");
        _output.WriteLine($"  Files: {archive.Entries.Count:N0}");
        _output.WriteLine($"  Size: {archive.SizeInBytes:N0} bytes");
        _output.WriteLine($"  SHA-256: {archive.Sha256}");
        _output.WriteLine();
        _output.WriteLine(
            "WARNING: Continuing will start live authentication and then send the ZIP once.");
        _output.WriteLine("The upload will not be retried automatically.");
        _output.Write($"Type {RequiredPhrase} exactly to continue: ");

        var response = _input.ReadLine();
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            string.Equals(response?.Trim(), RequiredPhrase, StringComparison.Ordinal));
    }
}
