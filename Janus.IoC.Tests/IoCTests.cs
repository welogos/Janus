using FluentValidation;
using Janus.Application.Features.CQRS.Commands.Endpoint;
using Janus.Application.Features.Handlers.Endpoint;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Dtos.Dtos.Endpoint;
using Janus.Infrastructure.Context;
using Janus.Infrastructure.Services.Endpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Janus.IoC.Tests;

public class IoCTests
{
    #region Fact Tests
    [Fact]
    public void WhenInfrastructureIsRegisteredHasRequiredServices()
    {
        using var serviceProvider = CreateServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.IsType<EndpointService>(scope.ServiceProvider.GetRequiredService<IEndpointService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IMediator>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRequestHandler<CreateEndpointCommand>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IValidator<EndpointDto>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    [Fact]
    public void WhenEndpointServiceIsResolvedHasScopedLifetime()
    {
        using var serviceProvider = CreateServiceProvider();
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();

        var firstResolution = firstScope.ServiceProvider.GetRequiredService<IEndpointService>();
        var secondResolution = firstScope.ServiceProvider.GetRequiredService<IEndpointService>();
        var anotherScopeResolution = secondScope.ServiceProvider.GetRequiredService<IEndpointService>();

        Assert.Same(firstResolution, secondResolution);
        Assert.NotSame(firstResolution, anotherScopeResolution);
    }

    [Fact]
    public void WhenEndpointRegistryIsResolvedHasSingletonLifetime()
    {
        using var serviceProvider = CreateServiceProvider();
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();

        var firstResolution = firstScope.ServiceProvider
            .GetRequiredService<IEndpointRegistry>();
        var secondResolution = secondScope.ServiceProvider
            .GetRequiredService<IEndpointRegistry>();

        Assert.IsType<EndpointRegistry>(firstResolution);
        Assert.Same(firstResolution, secondResolution);
    }

    [Fact]
    public void WhenDatabaseContextIsResolvedHasConfiguredPostgreSqlProvider()
    {
        using var serviceProvider = CreateServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.Equal(
            "Host=localhost;Port=5432;Database=JanusTests;Username=postgres;Password=postgres;",
            context.Database.GetConnectionString());
    }
    #endregion

    #region Private Methods
    private static ServiceProvider CreateServiceProvider()
    {
        const string connectionString =
            "Host=localhost;Port=5432;Database=JanusTests;Username=postgres;Password=postgres;";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.UpInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
    #endregion
}
