using Janus.Domain.Enums;

namespace Janus.Api.Tests.Routing;

public class HttpMethodConversionTests
{
    [Theory]
    [InlineData("GET", EHttpMethods.Get)]
    [InlineData("get", EHttpMethods.Get)]
    [InlineData("POST", EHttpMethods.Post)]
    [InlineData("PUT", EHttpMethods.Put)]
    [InlineData("DELETE", EHttpMethods.Delete)]
    public void WhenHttpMethodIsSupportedHasConvertedToExpectedEnum(
        string requestMethod,
        EHttpMethods expectedMethod)
    {
        var converted = Enum.TryParse<EHttpMethods>(
            requestMethod,
            ignoreCase: true,
            out var method);

        Assert.True(converted);
        Assert.Equal(expectedMethod, method);
    }

    [Theory]
    [InlineData("PATCH")]
    [InlineData("OPTIONS")]
    [InlineData("INVALID")]
    public void WhenHttpMethodIsUnsupportedHasNotConverted(string requestMethod)
    {
        var converted = Enum.TryParse<EHttpMethods>(
            requestMethod,
            ignoreCase: true,
            out _);

        Assert.False(converted);
    }
}
