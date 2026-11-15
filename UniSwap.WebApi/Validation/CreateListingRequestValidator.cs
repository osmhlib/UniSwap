using FluentValidation;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Validation;

public sealed class CreateListingRequestValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingRequestValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Title).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(value => value.Trim().Length is >= 3 and <= 100)
            .WithMessage("Title must contain 3 to 100 characters after trimming.");
        RuleFor(x => x.Description).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(value => value.Trim().Length <= 2000)
            .WithMessage("Description must contain at most 2000 characters after trimming.");
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.Condition).NotEmpty();
    }
}
