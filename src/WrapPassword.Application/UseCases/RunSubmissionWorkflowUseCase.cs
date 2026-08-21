using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;
using WrapPassword.Application.Services;

namespace WrapPassword.Application.UseCases;

public sealed class RunSubmissionWorkflowUseCase
{
    private readonly PrepareSubmissionArchiveUseCase _prepareArchive;
    private readonly ILiveSubmissionConfirmation _confirmation;
    private readonly AuthenticatePasswordCandidatesUseCase _authenticateCandidates;
    private readonly UploadSubmissionUseCase _uploadSubmission;

    public RunSubmissionWorkflowUseCase(
        PrepareSubmissionArchiveUseCase prepareArchive,
        ILiveSubmissionConfirmation confirmation,
        AuthenticatePasswordCandidatesUseCase authenticateCandidates,
        UploadSubmissionUseCase uploadSubmission)
    {
        _prepareArchive = prepareArchive
            ?? throw new ArgumentNullException(nameof(prepareArchive));
        _confirmation = confirmation
            ?? throw new ArgumentNullException(nameof(confirmation));
        _authenticateCandidates = authenticateCandidates
            ?? throw new ArgumentNullException(nameof(authenticateCandidates));
        _uploadSubmission = uploadSubmission
            ?? throw new ArgumentNullException(nameof(uploadSubmission));
    }

    public async Task<SubmissionWorkflowResult> ExecuteAsync(
        SubmissionWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var archive = await _prepareArchive.ExecuteAsync(
            request.RepositoryRoot,
            request.CvPath,
            request.ArchivePath,
            cancellationToken);

        var isConfirmed = await _confirmation.ConfirmAsync(archive, cancellationToken);

        if (!isConfirmed)
        {
            return new SubmissionWorkflowResult(
                archive,
                WasSubmitted: false,
                AuthenticationAttemptCount: 0,
                UploadResult: null);
        }

        var authentication = await _authenticateCandidates.ExecuteAsync(
            request.Username,
            cancellationToken);
        var uploadRequest = new SubmissionUploadRequest(
            authentication.UploadUri,
            archive.ArchivePath,
            request.Applicant);
        cancellationToken.ThrowIfCancellationRequested();

        // Once the single POST starts, wait for its outcome so cancellation
        // cannot create uncertainty that encourages a duplicate submission.
        var uploadResult = await _uploadSubmission.ExecuteAsync(
            uploadRequest,
            CancellationToken.None);

        return new SubmissionWorkflowResult(
            archive,
            WasSubmitted: true,
            authentication.AttemptCount,
            uploadResult);
    }

    private static void ValidateRequest(SubmissionWorkflowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CvPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ArchivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Username);
        ApplicantDetailsValidator.Validate(request.Applicant);
    }
}
