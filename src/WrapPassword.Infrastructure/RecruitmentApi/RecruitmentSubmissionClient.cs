using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WrapPassword.Application.Abstractions;
using WrapPassword.Application.Models;

namespace WrapPassword.Infrastructure.RecruitmentApi;

public sealed class RecruitmentSubmissionClient : ISubmissionUploader
{
    private const string SuccessMessage = "Success";

    private readonly HttpClient _httpClient;

    public RecruitmentSubmissionClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<SubmissionUploadResult> UploadAsync(
        SubmissionUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        cancellationToken.ThrowIfCancellationRequested();

        var archiveBytes = await SubmissionArchiveReader.ReadAsync(
            request.ArchivePath,
            cancellationToken);
        var payload = CreatePayload(request, archiveBytes);

        using var httpRequest = CreateHttpRequest(request.UploadUri, payload);
        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        return await ReadResultAsync(response, cancellationToken);
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

        RecruitmentUploadUriValidator.EnsureValid(request.UploadUri);
    }

    private static SubmissionPayload CreatePayload(
        SubmissionUploadRequest request,
        byte[] archiveBytes)
    {
        return new SubmissionPayload(
            Convert.ToBase64String(archiveBytes),
            request.Applicant.Name,
            request.Applicant.Surname,
            request.Applicant.Email);
    }

    private static HttpRequestMessage CreateHttpRequest(
        Uri uploadUri,
        SubmissionPayload payload)
    {
        var json = JsonSerializer.Serialize(payload);

        return new HttpRequestMessage(HttpMethod.Post, uploadUri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static async Task<SubmissionUploadResult> ReadResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException(
                $"The upload endpoint returned HTTP {(int)response.StatusCode} after one POST. "
                + "The request was not retried.",
                inner: null,
                response.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulResponse(responseBody);

        return new SubmissionUploadResult((int)response.StatusCode, SuccessMessage);
    }

    private static void EnsureSuccessfulResponse(string responseBody)
    {
        if (string.Equals(responseBody.Trim(), SuccessMessage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);

            if (TryReadSuccessMessage(document.RootElement))
            {
                return;
            }
        }
        catch (JsonException)
        {
            // The same safe error is returned for malformed text and malformed JSON.
        }

        throw new InvalidDataException(
            "The upload endpoint returned HTTP 200 without a Success message.");
    }

    private static bool TryReadSuccessMessage(JsonElement rootElement)
    {
        if (rootElement.ValueKind == JsonValueKind.String)
        {
            return string.Equals(
                rootElement.GetString(),
                SuccessMessage,
                StringComparison.OrdinalIgnoreCase);
        }

        if (rootElement.ValueKind != JsonValueKind.Object
            || !rootElement.TryGetProperty("message", out var messageElement)
            || messageElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        return string.Equals(
            messageElement.GetString(),
            SuccessMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SubmissionPayload(
        [property: JsonPropertyName("data")] string Data,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("surname")] string Surname,
        [property: JsonPropertyName("email")] string Email);
}
