using FluentValidation;
using FluentValidation.TestHelper;
using Janus.Application.Features.Validations.Endpoint;
using Janus.Domain.Enums;
using Janus.Dtos.Dtos.Endpoint;

namespace Janus.Application.Tests.Validations;

public class EndpointValidationTests
{
    #region Attributes
    private static readonly IValidator<EndpointDto> Validator = new EndpointDtoValidator();
    #endregion

    #region Theory Tests
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("api/endpoint")]
    [InlineData("something")]
    public void WhenRouteIsInvalidHasError(string route)
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.ClientRoute = route;

        var result = Validator.TestValidate(endpointDto);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(x => x.ClientRoute);
    }

    [Theory]
    [InlineData(EHttpMethods.Get)]
    [InlineData(EHttpMethods.Post)]
    [InlineData(EHttpMethods.Put)]
    [InlineData(EHttpMethods.Delete)]
    public void WhenMethodIsDefinedHasNoError(EHttpMethods method)
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.Method = method;

        var result = Validator.TestValidate(endpointDto);

        result.ShouldNotHaveValidationErrorFor(x => x.Method);
    }
    #endregion

    #region Fact Tests
    [Fact]
    public void WhenEndpointIsValidHasNoError()
    {
        var endpointDto = CreateValidEndpointDto();

        var result = Validator.TestValidate(endpointDto);

        Assert.True(result.IsValid);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void WhenClientNameIsEmptyHasError()
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.ClientName = string.Empty;

        var result = Validator.TestValidate(endpointDto);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(x => x.ClientName)
            .WithErrorMessage("Client name is required.");
    }

    [Fact]
    public void WhenRouteExceeds500CharactersHasError()
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.ClientRoute = "/" + new string('a', 500);

        var result = Validator.TestValidate(endpointDto);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(x => x.ClientRoute)
            .WithErrorMessage("Client route must not exceed 500 characters.");
    }

    [Fact]
    public void WhenRouteHas500CharactersHasNoError()
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.ClientRoute = "/" + new string('a', 499);

        var result = Validator.TestValidate(endpointDto);

        result.ShouldNotHaveValidationErrorFor(x => x.ClientRoute);
    }

    [Fact]
    public void WhenClientNameExceeds100CharactersHasError()
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.ClientName = new string('a', 101);

        var result = Validator.TestValidate(endpointDto);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(x => x.ClientName)
            .WithErrorMessage("Client name must not exceed 100 characters.");
    }

    [Fact]
    public void WhenClientNameHas100CharactersHasNoError()
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.ClientName = new string('a', 100);

        var result = Validator.TestValidate(endpointDto);

        result.ShouldNotHaveValidationErrorFor(x => x.ClientName);
    }

    [Fact]
    public void WhenMethodIsUndefinedHasError()
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.Method = (EHttpMethods)999;

        var result = Validator.TestValidate(endpointDto);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(x => x.Method)
            .WithErrorMessage("HTTP method is invalid.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("hermes/internal")]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://user:password@hermes/internal")]
    [InlineData("http://hermes/internal#fragment")]
    public void WhenTargetUrlIsNotAbsoluteHttpUrlHasError(string targetUrl)
    {
        var endpointDto = CreateValidEndpointDto();
        endpointDto.TargetUrl = targetUrl;

        var result = Validator.TestValidate(endpointDto);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(x => x.TargetUrl);
    }
    #endregion

    #region Private Methods
    private static EndpointDto CreateValidEndpointDto()
        => new()
        {
            ClientName = "ClientName",
            ClientRoute = "/ClientRoute",
            TargetUrl = "http://hermes:8080/api/messages",
            Method = EHttpMethods.Get,
            Enabled = true,
        };
    #endregion
}
