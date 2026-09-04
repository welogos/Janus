using Janus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Janus.Api.Extensions;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        try
        {
            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            logger.LogInformation("Checking database migrations...");

            var pendingMigrations = await context.Database
                .GetPendingMigrationsAsync(cancellationToken);

            var migrations = pendingMigrations.ToArray();

            if (migrations.Length == 0)
            {
                logger.LogInformation("Database is already up to date.");
                return;
            }

            logger.LogInformation(
                "Applying {MigrationCount} pending migration(s): {Migrations}",
                migrations.Length,
                string.Join(", ", migrations)
            );

            await context.Database.MigrateAsync(cancellationToken);

            logger.LogInformation(
                "Database migrations applied successfully."
            );
        }
        catch (Exception ex)
        {
            logger.LogCritical(
                ex,
                "An error occurred while applying database migrations."
            );

            throw;
        }
    }
}