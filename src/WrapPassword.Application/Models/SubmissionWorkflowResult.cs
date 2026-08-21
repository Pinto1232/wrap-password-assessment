namespace WrapPassword.Application.Models;

public sealed record SubmissionWorkflowResult(
    ArchiveBuildResult Archive,
    bool WasSubmitted,
    int AuthenticationAttemptCount,
    SubmissionUploadResult? UploadResult);
