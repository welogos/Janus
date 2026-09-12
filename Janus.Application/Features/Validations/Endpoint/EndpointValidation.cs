using FluentValidation;
using Janus.Dtos.Dtos.Endpoint;

namespace Janus.Application.Features.Validations.Endpoint;

public class EndpointDtoValidator : AbstractValidator<EndpointDto>
{
    public EndpointDtoValidator()
    {
        RuleFor(x => x.ClientName)
            .NotEmpty()
            .WithMessage("Client name is required.")
            .MaximumLength(100)
            .WithMessage("Client name must not exceed 100 characters.");

        RuleFor(x => x.ClientRoute)
            .NotEmpty()
            .WithMessage("Client route is required.")
            .MaximumLength(500)
            .WithMessage("Client route must not exceed 500 characters.")
            .Must(route => route.StartsWith('/'))
            .WithMessage("Client route must start with '/'.");

        RuleFor(x => x.TargetUrl)
            .NotEmpty()
            .WithMessage("Target URL is required.")
            .MaximumLength(2048)
            .WithMessage("Target URL must not exceed 2048 characters.")
            .Must(IsAbsoluteHttpUrl)
            .WithMessage("Target URL must be an absolute HTTP or HTTPS URL.");

        RuleFor(x => x.Method)
            .IsInEnum()
            .WithMessage("HTTP method is invalid.");
    }

    private static bool IsAbsoluteHttpUrl(string targetUrl)
        => Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri) &&
           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
           string.IsNullOrEmpty(uri.UserInfo) &&
           string.IsNullOrEmpty(uri.Fragment);
}
