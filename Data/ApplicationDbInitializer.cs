using Microsoft.EntityFrameworkCore;
using WrapPassword.Data.Entities;

namespace WrapPassword.Data;

public static class ApplicationDbInitializer
{
    private const string ApplicationName = "Wrap Password Assessment";

    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await database.Database.EnsureCreatedAsync(cancellationToken);

        if (await database.ApplicationMetadata.AnyAsync(cancellationToken))
        {
            return;
        }

        database.ApplicationMetadata.Add(new ApplicationMetadata
        {
            Name = ApplicationName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await database.SaveChangesAsync(cancellationToken);
    }
}
