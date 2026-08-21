using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WrapPassword.Application.Abstractions;

namespace WrapPassword.Infrastructure.RecruitmentApi;

public sealed class RecruitmentAuthenticationClient : IRecruitmentAuthenticationClient, IDisposable
{
    public const int DefaultRequestsPerSecond = 9;

    private const int MaximumRequestsPerSecond = 10;
    private static readonly Uri AuthenticationEndpoint = new(
        "https://recruitment.warpdevelopment.co.za/v2/api/authenticate");

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _minimumRequestInterval;
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private long? _lastRequestTimestamp;

    public RecruitmentAuthenticationClient(
        HttpClient httpClient,
        int requestsPerSecond = DefaultRequestsPerSecond,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        if (requestsPerSecond is < 1 or > MaximumRequestsPerSecond)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestsPerSecond),
                $"The request rate must be between 1 and {MaximumRequestsPerSecond} requests per second.");
        }

        _httpClient = httpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _minimumRequestInterval = TimeSpan.FromSeconds(1d / requestsPerSecond);
    }

    public async Task<Uri?> TryAuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(password);

        await _requestGate.WaitAsync(cancellationToken);

        try
        {
            await WaitForRequestSlotAsync(cancellationToken);

            using var request = CreateRequest(username, password);

            _lastRequestTimestamp = _timeProvider.GetTimestamp();

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return await ReadAuthenticationResultAsync(response, cancellationToken);
        }
        finally
        {
            _requestGate.Release();
        }
    }

    public void Dispose()
    {
        _requestGate.Dispose();
    }

    private static HttpRequestMessage CreateRequest(string username, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, AuthenticationEndpoint);
        request.Headers.Authorization = CreateBasicAuthorization(username, password);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static async Task<Uri?> ReadAuthenticationResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException(
                $"The authentication endpoint returned HTTP {(int)response.StatusCode}.",
                inner: null,
                response.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseUploadUri(responseBody);
    }

    private async Task WaitForRequestSlotAsync(CancellationToken cancellationToken)
    {
        if (_lastRequestTimestamp is null)
        {
            return;
        }

        var elapsed = _timeProvider.GetElapsedTime(_lastRequestTimestamp.Value);
        var remainingDelay = _minimumRequestInterval - elapsed;

        if (remainingDelay > TimeSpan.Zero)
        {
            await Task.Delay(remainingDelay, _timeProvider, cancellationToken);
        }
    }

    private static AuthenticationHeaderValue CreateBasicAuthorization(
        string username,
        string password)
    {
        var credentialBytes = Encoding.UTF8.GetBytes($"{username}:{password}");
        var encodedCredentials = Convert.ToBase64String(credentialBytes);

        return new AuthenticationHeaderValue("Basic", encodedCredentials);
    }

    private static Uri ParseUploadUri(string responseBody)
    {
        if (TryCreateValidUploadUri(responseBody.Trim(), out var directUri))
        {
            return directUri;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);

            foreach (var value in EnumerateStringValues(document.RootElement))
            {
                if (TryCreateValidUploadUri(value, out var jsonUri))
                {
                    return jsonUri;
                }
            }
        }
        catch (JsonException)
        {
            // A successful response may be plain text. Validation below reports
            // the same safe error for malformed text and malformed JSON.
        }

        throw new InvalidDataException(
            "The authentication response did not contain a valid temporary upload URL.");
    }

    private static IEnumerable<string> EnumerateStringValues(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var value = element.GetString();

                if (value is not null)
                {
                    yield return value;
                }

                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var nestedValue in EnumerateStringValues(property.Value))
                    {
                        yield return nestedValue;
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var nestedValue in EnumerateStringValues(item))
                    {
                        yield return nestedValue;
                    }
                }

                break;
        }
    }

    private static bool TryCreateValidUploadUri(string value, out Uri uploadUri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var candidate)
            && RecruitmentUploadUriValidator.IsValid(candidate))
        {
            uploadUri = candidate;
            return true;
        }

        uploadUri = null!;
        return false;
    }
}
