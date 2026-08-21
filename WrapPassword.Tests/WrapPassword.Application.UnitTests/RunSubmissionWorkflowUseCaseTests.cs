using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;
using WrapPassword.Application.Services;
using WrapPassword.Application.UseCases;
using Xunit;

namespace WrapPassword.Application.UnitTests;

public sealed class RunSubmissionWorkflowUseCaseTests
{
    private static readonly Uri UploadUri = new(
        "https://recruitment.warpdevelopment.co.za/v2/api/upload/test-token/");

    [Fact]
    public async Task ExecuteAsync_ConfirmedRequest_CompletesStepsInOrder()
    {
        var fixture = new WorkflowFixture(isConfirmed: true);
        var request = CreateRequest();

        var result = await fixture.UseCase.ExecuteAsync(request);

        Assert.Equal(
            ["generate", "prepare", "confirm", "authenticate", "upload"],
            fixture.Events);
        Assert.True(result.WasSubmitted);
        Assert.Equal(1, result.AuthenticationAttemptCount);
        Assert.Equal(fixture.ArchiveBuilder.Result, result.Archive);
        Assert.Equal(fixture.Uploader.Result, result.UploadResult);

        var uploadRequest = Assert.Single(fixture.Uploader.Requests);
        Assert.Equal(UploadUri, uploadRequest.UploadUri);
        Assert.Equal(fixture.ArchiveBuilder.Result.ArchivePath, uploadRequest.ArchivePath);
        Assert.Equal(request.Applicant, uploadRequest.Applicant);
        Assert.False(Assert.Single(fixture.Uploader.CancellationTokens).CanBeCanceled);
    }

    [Fact]
    public async Task ExecuteAsync_DeclinedRequest_StopsBeforeLiveRequests()
    {
        var fixture = new WorkflowFixture(isConfirmed: false);

        var result = await fixture.UseCase.ExecuteAsync(CreateRequest());

        Assert.Equal(["generate", "prepare", "confirm"], fixture.Events);
        Assert.False(result.WasSubmitted);
        Assert.Equal(0, result.AuthenticationAttemptCount);
        Assert.Null(result.UploadResult);
        Assert.Empty(fixture.AuthenticationClient.Attempts);
        Assert.Empty(fixture.Uploader.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidApplicant_StopsBeforePreparation()
    {
        var fixture = new WorkflowFixture(isConfirmed: true);
        var request = CreateRequest() with
        {
            Applicant = new ApplicantDetails("Pinto", "Manuel", "invalid-email")
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.UseCase.ExecuteAsync(request));

        Assert.Empty(fixture.Events);
    }

    private static SubmissionWorkflowRequest CreateRequest()
    {
        return new SubmissionWorkflowRequest(
            "/tmp/repository",
            "/tmp/cv.pdf",
            "/tmp/submission.zip",
            "John",
            new ApplicantDetails("Pinto", "Manuel", "pinto@example.com"));
    }

    private sealed class WorkflowFixture
    {
        public WorkflowFixture(bool isConfirmed)
        {
            var dictionaryWriter = new RecordingDictionaryWriter(Events);
            var generateDictionary = new GeneratePasswordDictionaryUseCase(
                new PasswordDictionaryGenerator(),
                dictionaryWriter);
            ArchiveBuilder = new RecordingArchiveBuilder(Events);
            var prepareArchive = new PrepareSubmissionArchiveUseCase(
                generateDictionary,
                ArchiveBuilder);
            var confirmation = new RecordingConfirmation(Events, isConfirmed);
            AuthenticationClient = new RecordingAuthenticationClient(Events);
            var authenticateCandidates = new AuthenticatePasswordCandidatesUseCase(
                new PasswordDictionaryGenerator(),
                AuthenticationClient);
            Uploader = new RecordingUploader(Events);
            var uploadSubmission = new UploadSubmissionUseCase(Uploader);

            UseCase = new RunSubmissionWorkflowUseCase(
                prepareArchive,
                confirmation,
                authenticateCandidates,
                uploadSubmission);
        }

        public List<string> Events { get; } = [];

        public RecordingArchiveBuilder ArchiveBuilder { get; }

        public RecordingAuthenticationClient AuthenticationClient { get; }

        public RecordingUploader Uploader { get; }

        public RunSubmissionWorkflowUseCase UseCase { get; }
    }

    private sealed class RecordingDictionaryWriter : IPasswordDictionaryWriter
    {
        private readonly ICollection<string> _events;

        public RecordingDictionaryWriter(ICollection<string> events)
        {
            _events = events;
        }

        public Task<string> WriteAsync(
            IEnumerable<string> candidates,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            _events.Add("generate");
            return Task.FromResult(outputPath);
        }
    }

    private sealed class RecordingArchiveBuilder : ISubmissionArchiveBuilder
    {
        private readonly ICollection<string> _events;

        public RecordingArchiveBuilder(ICollection<string> events)
        {
            _events = events;
        }

        public ArchiveBuildResult Result { get; } = new(
            "/tmp/submission.zip",
            1_024,
            "archive-sha256",
            []);

        public Task<ArchiveBuildResult> BuildAsync(
            SubmissionArchiveRequest request,
            CancellationToken cancellationToken = default)
        {
            _events.Add("prepare");
            return Task.FromResult(Result);
        }
    }

    private sealed class RecordingConfirmation : ILiveSubmissionConfirmation
    {
        private readonly ICollection<string> _events;
        private readonly bool _isConfirmed;

        public RecordingConfirmation(ICollection<string> events, bool isConfirmed)
        {
            _events = events;
            _isConfirmed = isConfirmed;
        }

        public Task<bool> ConfirmAsync(
            ArchiveBuildResult archive,
            CancellationToken cancellationToken = default)
        {
            _events.Add("confirm");
            return Task.FromResult(_isConfirmed);
        }
    }

    private sealed class RecordingAuthenticationClient : IRecruitmentAuthenticationClient
    {
        private readonly ICollection<string> _events;

        public RecordingAuthenticationClient(ICollection<string> events)
        {
            _events = events;
        }

        public List<(string Username, string Password)> Attempts { get; } = [];

        public Task<Uri?> TryAuthenticateAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            _events.Add("authenticate");
            Attempts.Add((username, password));
            return Task.FromResult<Uri?>(UploadUri);
        }
    }

    private sealed class RecordingUploader : ISubmissionUploader
    {
        private readonly ICollection<string> _events;

        public RecordingUploader(ICollection<string> events)
        {
            _events = events;
        }

        public SubmissionUploadResult Result { get; } = new(200, "Success");

        public List<SubmissionUploadRequest> Requests { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public Task<SubmissionUploadResult> UploadAsync(
            SubmissionUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            _events.Add("upload");
            Requests.Add(request);
            CancellationTokens.Add(cancellationToken);
            return Task.FromResult(Result);
        }
    }
}
