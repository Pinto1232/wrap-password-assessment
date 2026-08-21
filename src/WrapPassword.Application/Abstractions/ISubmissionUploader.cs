using WrapPassword.Application.Models;

namespace WrapPassword.Application.Abstractions;

public interface ISubmissionUploader
{
    Task<SubmissionUploadResult> UploadAsync(
        SubmissionUploadRequest request,
        CancellationToken cancellationToken = default);
}
