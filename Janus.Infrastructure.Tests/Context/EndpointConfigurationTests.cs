using Janus.Domain.Models;
using Janus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Janus.Infrastructure.Tests.Context;

public class EndpointConfigurationTests
{
    #region Fact Tests
    [Fact]
    public void WhenEndpointModelIsCreatedHasExpectedConfiguration()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(EndpointDomain));

        Assert.NotNull(entity);
        Assert.Equal("Endpoints", entity.GetTableName());
        Assert.Equal(nameof(EndpointDomain.Id), entity.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Equal(100, entity.FindProperty(nameof(EndpointDomain.ClientName))!.GetMaxLength());
        Assert.Equal(500, entity.FindProperty(nameof(EndpointDomain.ClientRoute))!.GetMaxLength());
        Assert.Equal(2048, entity.FindProperty(nameof(EndpointDomain.TargetUrl))!.GetMaxLength());
        Assert.Equal(10, entity.FindProperty(nameof(EndpointDomain.Method))!.GetMaxLength());
        Assert.Equal(typeof(string), entity.FindProperty(nameof(EndpointDomain.Method))!.GetProviderClrType());
    }

    [Fact]
    public void WhenEndpointModelIsCreatedHasUniqueMethodAndRouteIndex()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(EndpointDomain))!;

        var index = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(EndpointDomain.Method),
                    nameof(EndpointDomain.ClientRoute),
                ]));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void WhenEndpointModelIsCreatedHasEnabledIndex()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(EndpointDomain))!;

        var index = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Count == 1 &&
            candidate.Properties[0].Name == nameof(EndpointDomain.Enabled));

        Assert.False(index.IsUnique);
    }
    #endregion

    #region Private Methods
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
    #endregion
}
