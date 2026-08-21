using System.Security.Cryptography;
using System.Text;
using WrapPassword.Application.Services;
using Xunit;

namespace WrapPassword.RegressionTests;

public sealed class DictionaryRegressionTests
{
    private const string ExpectedSha256 =
        "93c2aa1e1b7355db0ebb40d262f9da19784ea19ed183fa67530788b525c2f433";

    [Fact]
    public void Generate_CanonicalDictionaryFingerprintHasNotChanged()
    {
        var candidates = new PasswordDictionaryGenerator().Generate();
        var canonicalDictionary = string.Join('\n', candidates);
        var bytes = Encoding.UTF8.GetBytes(canonicalDictionary);
        var fingerprint = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        Assert.Equal(ExpectedSha256, fingerprint);
    }

    [Fact]
    public void Generate_FirstAndLastCandidatesHaveNotChanged()
    {
        var candidates = new PasswordDictionaryGenerator().Generate().ToArray();

        Assert.Equal("password", candidates[0]);
        Assert.Equal("P@55W0RD", candidates[^1]);
    }
}
