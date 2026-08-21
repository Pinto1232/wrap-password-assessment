namespace WrapPassword.Application.Models;

public sealed record SubmissionArchiveRequest(
    string RepositoryRoot,
    string CvPath,
    string DictionaryPath,
    string OutputPath);
