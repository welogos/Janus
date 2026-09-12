using FluentValidation;
using FluentValidation.AspNetCore;
using Janus.Application.Features.Handlers.Endpoint;
using Janus.Application.Features.Validations.Endpoint;
using Janus.Application.Interfaces.Services.Endpoint;
using Janus.Application.Interfaces.Services.Dispatching;
using Janus.Infrastructure.Context;
using Janus.Infrastructure.Services.Endpoints;
using Janus.Infrastructure.Services.Dispatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Janus.IoC;

public static class IoC
{
    public static IServiceCollection UpInfrastructure(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddControllers();

        // Database 
        service.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        
        // Mediator
        service.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(CreateEndpointHandler).Assembly));
        
        // FluentValidation
        service.AddValidatorsFromAssembly(typeof(EndpointDtoValidator).Assembly);
        service.AddFluentValidationAutoValidation();
        
        // Services
        service.AddScoped<IEndpointService, EndpointService>();
        service.AddScoped<IEndpointResolver, EndpointResolver>();
        service.AddScoped<IEndpointDispatcher, EndpointDispatcher>();
        service.AddHttpClient(EndpointDispatcher.HttpClientName);
        
        // Cache
        service.AddMemoryCache();
        service.AddSingleton<IEndpointRegistry, EndpointRegistry>();
        
        return service;
    }
}
