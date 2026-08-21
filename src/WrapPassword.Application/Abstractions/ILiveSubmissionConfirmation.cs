using WrapPassword.Application.Models;

namespace WrapPassword.Application.Abstractions;

public interface ILiveSubmissionConfirmation
{
    Task<bool> ConfirmAsync(
        ArchiveBuildResult archive,
        CancellationToken cancellationToken = default);
}
