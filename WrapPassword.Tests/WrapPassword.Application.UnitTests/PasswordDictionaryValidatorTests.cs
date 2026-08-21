using WrapPassword.Application.Services;
using WrapPassword.Domain.Passwords;
using Xunit;

namespace WrapPassword.Application.UnitTests;

public sealed class PasswordDictionaryValidatorTests
{
    [Fact]
    public void Validate_ReturnsCompleteValidDictionary()
    {
        var candidates = new PasswordDictionaryGenerator().Generate();

        var result = PasswordDictionaryValidator.Validate(candidates);

        Assert.Equal(PasswordRules.ExpectedCandidateCount, result.Count);
    }

    [Fact]
    public void Validate_RejectsUnexpectedCandidateCount()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => PasswordDictionaryValidator.Validate(["password"]));

        Assert.Contains("exactly", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsDuplicateCandidates()
    {
        var duplicateCandidates = Enumerable.Repeat(
            "password",
            PasswordRules.ExpectedCandidateCount);

        var exception = Assert.Throws<InvalidOperationException>(
            () => PasswordDictionaryValidator.Validate(duplicateCandidates));

        Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsCandidateThatViolatesPasswordRules()
    {
        var candidates = new PasswordDictionaryGenerator().Generate().ToArray();
        candidates[0] = "xassword";

        var exception = Assert.Throws<InvalidOperationException>(
            () => PasswordDictionaryValidator.Validate(candidates));

        Assert.Contains("password rules", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
