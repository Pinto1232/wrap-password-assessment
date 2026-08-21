using WrapPassword.Application.Models;

namespace WrapPassword.Application.Abstractions;

public interface ISubmissionArchiveBuilder
{
    Task<ArchiveBuildResult> BuildAsync(
        SubmissionArchiveRequest request,
        CancellationToken cancellationToken = default);
}
