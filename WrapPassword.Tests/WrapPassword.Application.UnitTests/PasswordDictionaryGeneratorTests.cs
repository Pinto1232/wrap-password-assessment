using WrapPassword.Application.Services;
using WrapPassword.Domain.Passwords;
using Xunit;

namespace WrapPassword.Application.UnitTests;

public sealed class PasswordDictionaryGeneratorTests
{
    private readonly PasswordDictionaryGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsExpectedNumberOfUniqueCandidates()
    {
        var candidates = _generator.Generate().ToArray();

        Assert.Equal(PasswordRules.ExpectedCandidateCount, candidates.Length);
        Assert.Equal(
            PasswordRules.ExpectedCandidateCount,
            candidates.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("P@55w0rd")]
    public void Generate_IncludesExpectedCandidate(string expectedCandidate)
    {
        Assert.Contains(expectedCandidate, _generator.Generate());
    }

    [Fact]
    public void Generate_UsesOnlyAllowedCharactersAtEachPosition()
    {
        var candidates = _generator.Generate();

        Assert.All(candidates, candidate =>
        {
            Assert.Equal(PasswordRules.CharacterOptions.Count, candidate.Length);

            for (var position = 0; position < candidate.Length; position++)
            {
                var allowedCharacters = PasswordRules.CharacterOptions[position];
                Assert.True(
                    allowedCharacters.Contains(candidate[position]),
                    $"'{candidate}' has an invalid character at position {position}.");
            }
        });
    }

    [Fact]
    public void Generate_ReturnsCandidatesInDeterministicOrder()
    {
        var firstRun = _generator.Generate().ToArray();
        var secondRun = _generator.Generate().ToArray();

        Assert.Equal(firstRun, secondRun);
    }
}
