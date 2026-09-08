using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Janus.Api.Extensions;

public static class DatabaseMigrationExtensions
{
    extension(WebApplication app)
    {
        public async Task MigrateDatabaseAsync(CancellationToken cancellationToken = default)
        {
            await using var scope = app.Services.CreateAsyncScope();

            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseMigration");

            try
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                logger.LogInformation("Checking for pending database migrations.");

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
            catch (Exception exception)
            {
                logger.LogCritical(
                    exception,
                    "Database migration failed."
                );

                throw;
            }
        }

        public async Task LoadEndpointsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                app.Logger.LogInformation("Initializing the endpoint registry.");

                await using var scope = app.Services.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var registry = scope.ServiceProvider.GetRequiredService<IEndpointRegistry>();

                var endpoints = await context.Endpoints
                    .Where(endpoint => endpoint.Enabled)
                    .ToListAsync(cancellationToken);

                await registry.LoadEndpointsAsync(endpoints);

                app.Logger.LogInformation(
                    "Endpoint registry initialization completed with {EndpointCount} enabled endpoints.",
                    endpoints.Count);
            }
            catch (Exception exception)
            {
                app.Logger.LogCritical(
                    exception,
                    "Endpoint registry initialization failed.");

                throw;
            }
        }
    }
}
