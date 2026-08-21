using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;
using WrapPassword.Application.UseCases;
using Xunit;

namespace WrapPassword.Application.UnitTests;

public sealed class UploadSubmissionUseCaseTests
{
    private static readonly Uri UploadUri = new(
        "https://recruitment.warpdevelopment.co.za/v2/api/upload/test-token/");

    [Fact]
    public async Task ExecuteAsync_DelegatesValidatedRequest()
    {
        var expectedResult = new SubmissionUploadResult(200, "Success");
        var uploader = new RecordingSubmissionUploader(expectedResult);
        var useCase = new UploadSubmissionUseCase(uploader);
        var request = CreateRequest();

        var result = await useCase.ExecuteAsync(request);

        Assert.Equal(expectedResult, result);
        Assert.Same(request, Assert.Single(uploader.Requests));
    }

    [Theory]
    [InlineData("", "Manuel", "pinto@example.com")]
    [InlineData("Pinto", "", "pinto@example.com")]
    [InlineData("Pinto", "Manuel", "not-an-email")]
    public async Task ExecuteAsync_RejectsInvalidApplicantDetails(
        string name,
        string surname,
        string email)
    {
        var uploader = new RecordingSubmissionUploader(
            new SubmissionUploadResult(200, "Success"));
        var useCase = new UploadSubmissionUseCase(uploader);
        var request = new SubmissionUploadRequest(
            UploadUri,
            "submission.zip",
            new ApplicantDetails(name, surname, email));

        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(request));

        Assert.Empty(uploader.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsRelativeUploadUrl()
    {
        var uploader = new RecordingSubmissionUploader(
            new SubmissionUploadResult(200, "Success"));
        var useCase = new UploadSubmissionUseCase(uploader);
        var request = CreateRequest(new Uri("/v2/api/upload/test-token/", UriKind.Relative));

        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(request));

        Assert.Empty(uploader.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_HonorsCancellationBeforeUpload()
    {
        var uploader = new RecordingSubmissionUploader(
            new SubmissionUploadResult(200, "Success"));
        var useCase = new UploadSubmissionUseCase(uploader);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => useCase.ExecuteAsync(CreateRequest(), cancellationSource.Token));

        Assert.Empty(uploader.Requests);
    }

    private static SubmissionUploadRequest CreateRequest(Uri? uploadUri = null)
    {
        return new SubmissionUploadRequest(
            uploadUri ?? UploadUri,
            "submission.zip",
            new ApplicantDetails("Pinto", "Manuel", "pinto@example.com"));
    }

    private sealed class RecordingSubmissionUploader : ISubmissionUploader
    {
        private readonly SubmissionUploadResult _result;

        public RecordingSubmissionUploader(SubmissionUploadResult result)
        {
            _result = result;
        }

        public List<SubmissionUploadRequest> Requests { get; } = [];

        public Task<SubmissionUploadResult> UploadAsync(
            SubmissionUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_result);
        }
    }
}
