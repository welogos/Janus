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

        RuleFor(x => x.Method)
            .IsInEnum()
            .WithMessage("HTTP method is invalid.");
    }
}