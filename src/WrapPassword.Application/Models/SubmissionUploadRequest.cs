namespace WrapPassword.Application.Models;

public sealed record SubmissionUploadRequest(
    Uri UploadUri,
    string ArchivePath,
    ApplicantDetails Applicant);
