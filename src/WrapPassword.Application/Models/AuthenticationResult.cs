namespace WrapPassword.Application.Models;

public sealed record AuthenticationResult(Uri UploadUri, int AttemptCount);
