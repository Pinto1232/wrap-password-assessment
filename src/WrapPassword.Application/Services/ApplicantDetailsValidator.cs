using System.Net.Mail;
using WrapPassword.Application.Models;

namespace WrapPassword.Application.Services;

public static class ApplicantDetailsValidator
{
    public static void Validate(ApplicantDetails applicant)
    {
        ArgumentNullException.ThrowIfNull(applicant);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicant.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicant.Surname);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicant.Email);

        if (!MailAddress.TryCreate(applicant.Email, out var parsedEmail)
            || !string.Equals(
                parsedEmail.Address,
                applicant.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The applicant email address is invalid.");
        }
    }
}
