using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Janus.IoC;

public static class IoC
{
    public static IServiceCollection UpInfrastructure(this IServiceCollection service, IConfiguration configuration)
    { service.AddControllers(); return service; }
}