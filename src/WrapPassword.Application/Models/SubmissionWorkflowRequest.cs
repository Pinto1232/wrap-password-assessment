namespace WrapPassword.Application.Models;

public sealed record SubmissionWorkflowRequest(
    string RepositoryRoot,
    string CvPath,
    string ArchivePath,
    string Username,
    ApplicantDetails Applicant);
