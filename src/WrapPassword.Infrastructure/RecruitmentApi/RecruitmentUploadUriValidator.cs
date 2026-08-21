namespace WrapPassword.Infrastructure.RecruitmentApi;

internal static class RecruitmentUploadUriValidator
{
    private const string UploadHost = "recruitment.warpdevelopment.co.za";
    private const string UploadPathPrefix = "/v2/api/upload/";

    public static bool IsValid(Uri candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return candidate.IsAbsoluteUri
            && string.Equals(
                candidate.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.IdnHost, UploadHost, StringComparison.OrdinalIgnoreCase)
            && candidate.IsDefaultPort
            && string.IsNullOrEmpty(candidate.UserInfo)
            && string.IsNullOrEmpty(candidate.Fragment)
            && candidate.AbsolutePath.StartsWith(UploadPathPrefix, StringComparison.Ordinal)
            && candidate.AbsolutePath.Length > UploadPathPrefix.Length;
    }

    public static void EnsureValid(Uri candidate)
    {
        if (!IsValid(candidate))
        {
            throw new ArgumentException(
                "The upload URL is not an expected recruitment upload URL.");
        }
    }
}
