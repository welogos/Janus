using Janus.Domain.Enums;
using Janus.Domain.Models;

namespace Janus.Domain.Tests.Models;

public class EndpointDomainTests
{
    #region Theory Tests
    [Theory]
    [InlineData(EHttpMethods.Get)]
    [InlineData(EHttpMethods.Post)]
    [InlineData(EHttpMethods.Put)]
    [InlineData(EHttpMethods.Delete)]
    public void WhenEndpointIsCreatedHasProvidedMethod(EHttpMethods method)
    {
        var endpoint = new EndpointDomain("Client", "/route", method);

        Assert.Equal(method, endpoint.Method);
    }
    #endregion

    #region Fact Tests
    [Fact]
    public void WhenEndpointIsCreatedHasProvidedValues()
    {
        var startedAt = DateTime.UtcNow;

        var endpoint = new EndpointDomain(
            "Portfolio",
            "/api/chat",
            EHttpMethods.Post,
            false,
            "http://hermes:8080/api/chat");

        var finishedAt = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, endpoint.Id);
        Assert.Equal("Portfolio", endpoint.ClientName);
        Assert.Equal("/api/chat", endpoint.ClientRoute);
        Assert.Equal(EHttpMethods.Post, endpoint.Method);
        Assert.Equal("http://hermes:8080/api/chat", endpoint.TargetUrl);
        Assert.False(endpoint.Enabled);
        Assert.InRange(endpoint.CreatedAt, startedAt, finishedAt);
        Assert.InRange(endpoint.UpdatedAt, startedAt, finishedAt);
        Assert.Equal(DateTimeKind.Utc, endpoint.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, endpoint.UpdatedAt.Kind);
    }

    [Fact]
    public void WhenEnabledIsNotProvidedHasTrueAsDefault()
    {
        var endpoint = new EndpointDomain(
            "Portfolio",
            "/api/chat",
            EHttpMethods.Post);

        Assert.True(endpoint.Enabled);
    }

    [Fact]
    public void WhenRouteHasEquivalentFormattingHasStoredCanonicalRoute()
    {
        var endpoint = new EndpointDomain(
            "Portfolio",
            " /API/Chat/ ",
            EHttpMethods.Post);

        Assert.Equal("/api/chat", endpoint.ClientRoute);
    }
    #endregion
}
