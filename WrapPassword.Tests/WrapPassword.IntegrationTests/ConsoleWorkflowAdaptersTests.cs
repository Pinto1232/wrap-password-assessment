using WrapPassword.Application.Models;
using WrapPassword.Cli;
using Xunit;

namespace WrapPassword.IntegrationTests;

public sealed class ConsoleWorkflowAdaptersTests
{
    [Fact]
    public void EnvironmentReader_ConfiguredVariables_ReturnsSettings()
    {
        var variables = CreateVariables();
        var reader = new SubmissionEnvironmentReader(
            variableName => variables.GetValueOrDefault(variableName));

        var result = reader.Read();

        Assert.Equal("John", result.Username);
        Assert.Equal("Pinto", result.Applicant.Name);
        Assert.Equal("Manuel", result.Applicant.Surname);
        Assert.Equal("pinto@example.com", result.Applicant.Email);
    }

    [Fact]
    public void EnvironmentReader_MissingUsername_UsesAssessmentDefault()
    {
        var variables = CreateVariables();
        variables.Remove(SubmissionEnvironmentReader.UsernameVariable);
        var reader = new SubmissionEnvironmentReader(
            variableName => variables.GetValueOrDefault(variableName));

        var result = reader.Read();

        Assert.Equal(SubmissionEnvironmentReader.DefaultUsername, result.Username);
    }

    [Theory]
    [InlineData(SubmissionEnvironmentReader.NameVariable)]
    [InlineData(SubmissionEnvironmentReader.SurnameVariable)]
    [InlineData(SubmissionEnvironmentReader.EmailVariable)]
    public void EnvironmentReader_MissingApplicantVariable_ReportsVariableName(
        string missingVariable)
    {
        var variables = CreateVariables();
        variables.Remove(missingVariable);
        var reader = new SubmissionEnvironmentReader(
            variableName => variables.GetValueOrDefault(variableName));

        var exception = Assert.Throws<InvalidOperationException>(() => reader.Read());

        Assert.Contains(missingVariable, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Confirmation_ExactPhrase_ConfirmsWithoutDisplayingArchivePath()
    {
        using var input = new StringReader("SUBMIT\n");
        using var output = new StringWriter();
        var confirmation = new ConsoleLiveSubmissionConfirmation(input, output);
        var archive = CreateArchiveResult();

        var result = await confirmation.ConfirmAsync(archive);

        Assert.True(result);
        Assert.Contains(archive.Sha256, output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(archive.ArchivePath, output.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("submit")]
    [InlineData("SUBMIT now")]
    [InlineData("")]
    public async Task Confirmation_AnythingExceptExactPhrase_Declines(string inputText)
    {
        using var input = new StringReader($"{inputText}\n");
        using var output = new StringWriter();
        var confirmation = new ConsoleLiveSubmissionConfirmation(input, output);

        var result = await confirmation.ConfirmAsync(CreateArchiveResult());

        Assert.False(result);
    }

    private static Dictionary<string, string> CreateVariables()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SubmissionEnvironmentReader.UsernameVariable] = "John",
            [SubmissionEnvironmentReader.NameVariable] = "Pinto",
            [SubmissionEnvironmentReader.SurnameVariable] = "Manuel",
            [SubmissionEnvironmentReader.EmailVariable] = "pinto@example.com",
        };
    }

    private static ArchiveBuildResult CreateArchiveResult()
    {
        return new ArchiveBuildResult(
            "/sensitive/local/submission.zip",
            1_024,
            "archive-sha256",
            []);
    }
}
