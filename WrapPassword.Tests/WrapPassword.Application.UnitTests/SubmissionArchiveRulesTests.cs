using WrapPassword.Domain.Submissions;
using Xunit;

namespace WrapPassword.Application.UnitTests;

public sealed class SubmissionArchiveRulesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4_999_999)]
    public void EnsureSizeIsAllowed_AcceptsSizeBelowLimit(long sizeInBytes)
    {
        SubmissionArchiveRules.EnsureSizeIsAllowed(sizeInBytes);
    }

    [Theory]
    [InlineData(5_000_000)]
    [InlineData(5_000_001)]
    public void EnsureSizeIsAllowed_RejectsSizeAtOrAboveLimit(long sizeInBytes)
    {
        Assert.Throws<InvalidOperationException>(
            () => SubmissionArchiveRules.EnsureSizeIsAllowed(sizeInBytes));
    }

    [Fact]
    public void EnsureSizeIsAllowed_RejectsNegativeSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SubmissionArchiveRules.EnsureSizeIsAllowed(-1));
    }
}
