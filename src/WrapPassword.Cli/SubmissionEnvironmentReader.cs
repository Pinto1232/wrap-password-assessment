using WrapPassword.Application.Models;

namespace WrapPassword.Cli;

public sealed class SubmissionEnvironmentReader
{
    public const string UsernameVariable = "WRAP_PASSWORD_USERNAME";
    public const string NameVariable = "WRAP_PASSWORD_NAME";
    public const string SurnameVariable = "WRAP_PASSWORD_SURNAME";
    public const string EmailVariable = "WRAP_PASSWORD_EMAIL";
    public const string DefaultUsername = "John";

    private readonly Func<string, string?> _readVariable;

    public SubmissionEnvironmentReader()
        : this(Environment.GetEnvironmentVariable)
    {
    }

    public SubmissionEnvironmentReader(Func<string, string?> readVariable)
    {
        _readVariable = readVariable
            ?? throw new ArgumentNullException(nameof(readVariable));
    }

    public SubmissionEnvironmentSettings Read()
    {
        var configuredUsername = _readVariable(UsernameVariable);
        var username = string.IsNullOrWhiteSpace(configuredUsername)
            ? DefaultUsername
            : configuredUsername;
        var applicant = new ApplicantDetails(
            ReadRequired(NameVariable),
            ReadRequired(SurnameVariable),
            ReadRequired(EmailVariable));

        return new SubmissionEnvironmentSettings(username, applicant);
    }

    private string ReadRequired(string variableName)
    {
        var value = _readVariable(variableName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required environment variable {variableName} is not configured.");
        }

        return value;
    }
}

public sealed record SubmissionEnvironmentSettings(
    string Username,
    ApplicantDetails Applicant);
