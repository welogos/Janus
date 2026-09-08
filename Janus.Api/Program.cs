using Janus.Api.ExceptionHandling;
using Janus.Api.Extensions;
using Janus.IoC;

var builder = WebApplication.CreateBuilder(args);
builder.Services.UpInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

await app.MigrateDatabaseAsync();
await app.LoadEndpointsAsync();
await app.EndpointFallBackAsync();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
