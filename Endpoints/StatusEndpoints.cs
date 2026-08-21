using Microsoft.EntityFrameworkCore;
using WrapPassword.Contracts;
using WrapPassword.Data;

namespace WrapPassword.Endpoints;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/status", GetStatusAsync);
        return endpoints;
    }

    private static async Task<IResult> GetStatusAsync(
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var metadata = await database.ApplicationMetadata
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        var status = new ApplicationStatus(
            metadata.Name,
            "Available",
            "SQLite",
            DateTimeOffset.UtcNow);

        return Results.Ok(status);
    }
}
