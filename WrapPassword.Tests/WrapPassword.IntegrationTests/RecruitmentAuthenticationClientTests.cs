using System.Diagnostics;
using System.Net;
using System.Text;
using WrapPassword.Infrastructure.RecruitmentApi;
using Xunit;

namespace WrapPassword.IntegrationTests;

public sealed class RecruitmentAuthenticationClientTests
{
    private static readonly Uri AuthenticationEndpoint = new(
        "https://recruitment.warpdevelopment.co.za/v2/api/authenticate");

    [Fact]
    public async Task TryAuthenticateAsync_SendsGetWithBasicAuthorization()
    {
        var handler = new RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var httpClient = new HttpClient(handler);
        using var client = new RecruitmentAuthenticationClient(httpClient);

        var result = await client.TryAuthenticateAsync("John", "P@55w0rd");

        Assert.Null(result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(AuthenticationEndpoint, request.Uri);
        Assert.Equal("Basic", request.AuthorizationScheme);
        Assert.Equal("John:P@55w0rd", DecodeBase64(request.AuthorizationParameter));
    }

    [Fact]
    public async Task TryAuthenticateAsync_ReturnsValidatedUploadUriFromJsonResponse()
    {
        const string uploadUrl =
            "https://recruitment.warpdevelopment.co.za/v2/api/upload/test-token/";
        var handler = new RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"uploadUrl\":\"{uploadUrl}\"}}")
            });
        using var httpClient = new HttpClient(handler);
        using var client = new RecruitmentAuthenticationClient(httpClient);

        var result = await client.TryAuthenticateAsync("John", "password");

        Assert.Equal(new Uri(uploadUrl), result);
    }

    [Theory]
    [InlineData("https://example.com/v2/api/upload/test-token/")]
    [InlineData("http://recruitment.warpdevelopment.co.za/v2/api/upload/test-token/")]
    [InlineData("https://recruitment.warpdevelopment.co.za/not-upload/test-token/")]
    public async Task TryAuthenticateAsync_RejectsUnexpectedUploadUrl(string uploadUrl)
    {
        var handler = new RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(uploadUrl)
            });
        using var httpClient = new HttpClient(handler);
        using var client = new RecruitmentAuthenticationClient(httpClient);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => client.TryAuthenticateAsync("John", "password"));
    }

    [Fact]
    public async Task TryAuthenticateAsync_RejectsUnexpectedStatusCode()
    {
        var handler = new RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Found));
        using var httpClient = new HttpClient(handler);
        using var client = new RecruitmentAuthenticationClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.TryAuthenticateAsync("John", "password"));

        Assert.Equal(HttpStatusCode.Found, exception.StatusCode);
    }

    [Fact]
    public async Task TryAuthenticateAsync_SpacesRequestsToConfiguredRate()
    {
        var handler = new RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var httpClient = new HttpClient(handler);
        using var client = new RecruitmentAuthenticationClient(
            httpClient,
            requestsPerSecond: 10);
        var stopwatch = Stopwatch.StartNew();

        await client.TryAuthenticateAsync("John", "candidate-1");
        await client.TryAuthenticateAsync("John", "candidate-2");
        await client.TryAuthenticateAsync("John", "candidate-3");

        stopwatch.Stop();
        Assert.True(
            stopwatch.Elapsed >= TimeSpan.FromMilliseconds(180),
            $"Three requests completed too quickly: {stopwatch.Elapsed.TotalMilliseconds:N0} ms.");
    }

    private static string DecodeBase64(string? value)
    {
        Assert.NotNull(value);
        return Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public RecordingHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<RequestSnapshot> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RequestSnapshot(
                request.Method,
                request.RequestUri,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter));

            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed record RequestSnapshot(
        HttpMethod Method,
        Uri? Uri,
        string? AuthorizationScheme,
        string? AuthorizationParameter);
}
