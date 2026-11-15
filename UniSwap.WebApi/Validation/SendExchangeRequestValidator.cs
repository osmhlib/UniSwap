using FluentValidation;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Validation;

public sealed class SendExchangeRequestValidator : AbstractValidator<SendExchangeRequestDto>
{
    public SendExchangeRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty();
        RuleFor(x => x.OfferedListingId).NotEmpty().NotEqual(x => x.ListingId)
            .WithMessage("The offered listing must differ from the requested listing.");
        RuleFor(x => x.RequesterId).NotEmpty();
    }
}
