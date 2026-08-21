using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using WrapPassword.Application.Models;
using WrapPassword.Infrastructure.RecruitmentApi;
using Xunit;

namespace WrapPassword.IntegrationTests;

public sealed class RecruitmentSubmissionClientTests
{
    private static readonly Uri UploadUri = new(
        "https://recruitment.warpdevelopment.co.za/v2/api/upload/test-token/");

    [Fact]
    public async Task UploadAsync_SendsExactPayloadOnce()
    {
        using var archive = TemporaryZipArchive.Create();
        var handler = new RecordingHttpMessageHandler(
            (_, _) => Task.FromResult(CreateSuccessResponse()));
        using var httpClient = new HttpClient(handler);
        var client = new RecruitmentSubmissionClient(httpClient);
        var request = CreateRequest(archive.Path);

        var result = await client.UploadAsync(request);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Success", result.Message);

        var recordedRequest = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recordedRequest.Method);
        Assert.Equal(UploadUri, recordedRequest.Uri);
        Assert.Equal("application/json", recordedRequest.ContentType);

        using var document = JsonDocument.Parse(recordedRequest.Body);
        var root = document.RootElement;
        var propertyNames = root.EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["data", "name", "surname", "email"], propertyNames);
        Assert.Equal("Pinto", root.GetProperty("name").GetString());
        Assert.Equal("Manuel", root.GetProperty("surname").GetString());
        Assert.Equal("pinto@example.com", root.GetProperty("email").GetString());

        var encodedArchive = root.GetProperty("data").GetString();
        Assert.NotNull(encodedArchive);
        var actualArchiveBytes = Convert.FromBase64String(encodedArchive);
        var expectedArchiveBytes = await File.ReadAllBytesAsync(archive.Path);
        Assert.Equal(expectedArchiveBytes, actualArchiveBytes);
    }

    [Theory]
    [InlineData("https://example.com/v2/api/upload/test-token/")]
    [InlineData("http://recruitment.warpdevelopment.co.za/v2/api/upload/test-token/")]
    [InlineData("https://recruitment.warpdevelopment.co.za/not-upload/test-token/")]
    public async Task UploadAsync_RejectsUnexpectedUploadUrlWithoutPost(string uploadUrl)
    {
        using var archive = TemporaryZipArchive.Create();
        var handler = new RecordingHttpMessageHandler(
            (_, _) => Task.FromResult(CreateSuccessResponse()));
        using var httpClient = new HttpClient(handler);
        var client = new RecruitmentSubmissionClient(httpClient);
        var request = CreateRequest(archive.Path, new Uri(uploadUrl));

        await Assert.ThrowsAsync<ArgumentException>(() => client.UploadAsync(request));

        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task UploadAsync_DoesNotRetryFailedResponse(HttpStatusCode statusCode)
    {
        using var archive = TemporaryZipArchive.Create();
        var handler = new RecordingHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(statusCode)));
        using var httpClient = new HttpClient(handler);
        var client = new RecruitmentSubmissionClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.UploadAsync(CreateRequest(archive.Path)));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task UploadAsync_DoesNotRetryTimeout()
    {
        using var archive = TemporaryZipArchive.Create();
        var handler = new RecordingHttpMessageHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(
                new TaskCanceledException("Simulated timeout.")));
        using var httpClient = new HttpClient(handler);
        var client = new RecruitmentSubmissionClient(httpClient);

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => client.UploadAsync(CreateRequest(archive.Path)));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task UploadAsync_RejectsUnconfirmedSuccessWithoutRetry()
    {
        using var archive = TemporaryZipArchive.Create();
        var handler = new RecordingHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"message\":\"Pending\"}")
            }));
        using var httpClient = new HttpClient(handler);
        var client = new RecruitmentSubmissionClient(httpClient);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => client.UploadAsync(CreateRequest(archive.Path)));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task UploadAsync_RejectsUnreadableArchiveWithoutPost()
    {
        using var archive = TemporaryZipArchive.CreateInvalid();
        var handler = new RecordingHttpMessageHandler(
            (_, _) => Task.FromResult(CreateSuccessResponse()));
        using var httpClient = new HttpClient(handler);
        var client = new RecruitmentSubmissionClient(httpClient);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => client.UploadAsync(CreateRequest(archive.Path)));

        Assert.Empty(handler.Requests);
    }

    private static SubmissionUploadRequest CreateRequest(
        string archivePath,
        Uri? uploadUri = null)
    {
        return new SubmissionUploadRequest(
            uploadUri ?? UploadUri,
            archivePath,
            new ApplicantDetails("Pinto", "Manuel", "pinto@example.com"));
    }

    private static HttpResponseMessage CreateSuccessResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":\"Success\"}")
        };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> _responseFactory;

        public RecordingHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<RequestSnapshot> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RequestSnapshot(
                request.Method,
                request.RequestUri,
                request.Content?.Headers.ContentType?.MediaType,
                body));

            return await _responseFactory(request, cancellationToken);
        }
    }

    private sealed record RequestSnapshot(
        HttpMethod Method,
        Uri? Uri,
        string? ContentType,
        string Body);

    private sealed class TemporaryZipArchive : IDisposable
    {
        private readonly string _directoryPath;

        private TemporaryZipArchive(string content)
        {
            _directoryPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"wrap-password-upload-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_directoryPath);
            Path = System.IO.Path.Combine(_directoryPath, "submission.zip");

            if (string.Equals(content, "valid", StringComparison.Ordinal))
            {
                using var zip = ZipFile.Open(Path, ZipArchiveMode.Create);
                var entry = zip.CreateEntry("dict.txt");
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write("password\n");
            }
            else
            {
                File.WriteAllText(Path, content);
            }
        }

        public string Path { get; }

        public static TemporaryZipArchive Create() => new("valid");

        public static TemporaryZipArchive CreateInvalid() => new("not a zip");

        public void Dispose()
        {
            if (Directory.Exists(_directoryPath))
            {
                Directory.Delete(_directoryPath, recursive: true);
            }
        }
    }
}
