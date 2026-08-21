namespace WrapPassword.Domain.Submissions;

public static class SubmissionArchiveRules
{
    public const long MaximumSizeInBytes = 5_000_000;

    public static void EnsureSizeIsAllowed(long sizeInBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes);

        if (sizeInBytes >= MaximumSizeInBytes)
        {
            throw new InvalidOperationException(
                $"The submission ZIP must be smaller than {MaximumSizeInBytes:N0} bytes.");
        }
    }
}
