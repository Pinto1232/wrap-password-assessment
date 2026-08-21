using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;
using WrapPassword.Application.Services;

namespace WrapPassword.Application.UseCases;

public sealed class UploadSubmissionUseCase
{
    private readonly ISubmissionUploader _uploader;

    public UploadSubmissionUseCase(ISubmissionUploader uploader)
    {
        _uploader = uploader ?? throw new ArgumentNullException(nameof(uploader));
    }

    public async Task<SubmissionUploadResult> ExecuteAsync(
        SubmissionUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        cancellationToken.ThrowIfCancellationRequested();

        return await _uploader.UploadAsync(request, cancellationToken);
    }

    private static void ValidateRequest(SubmissionUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.UploadUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ArchivePath);
        ApplicantDetailsValidator.Validate(request.Applicant);

        if (!request.UploadUri.IsAbsoluteUri)
        {
            throw new ArgumentException("The upload URL must be absolute.");
        }
    }
}
