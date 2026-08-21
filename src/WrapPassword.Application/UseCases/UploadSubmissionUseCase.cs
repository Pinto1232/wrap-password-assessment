using System.Net.Mail;
using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;

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
        ArgumentNullException.ThrowIfNull(request.Applicant);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Applicant.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Applicant.Surname);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Applicant.Email);

        if (!request.UploadUri.IsAbsoluteUri)
        {
            throw new ArgumentException("The upload URL must be absolute.");
        }

        if (!MailAddress.TryCreate(request.Applicant.Email, out var parsedEmail)
            || !string.Equals(
                parsedEmail.Address,
                request.Applicant.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The applicant email address is invalid.");
        }
    }
}
