using Janus.Api.Extensions;
using Janus.IoC;

var builder = WebApplication.CreateBuilder(args);
builder.Services.UpInfrastructure(builder.Configuration);

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();