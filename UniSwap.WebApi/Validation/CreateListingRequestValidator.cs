using FluentValidation;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Validation;

public sealed class CreateListingRequestValidator : ListingInputValidator<CreateListingRequest>;
public sealed class UpdateListingRequestValidator : ListingInputValidator<UpdateListingRequest>;

public abstract class ListingInputValidator<T> : AbstractValidator<T> where T : IListingInput
{
    protected ListingInputValidator()
    {
        RuleFor(x => x.Title).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(value => value.Trim().Length is >= 3 and <= 100)
            .WithMessage("Title must contain 3 to 100 characters after trimming.");
        RuleFor(x => x.Description).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(value => value.Trim().Length <= 2000)
            .WithMessage("Description must contain at most 2000 characters after trimming.");
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Condition).NotEmpty().MaximumLength(100);
    }
}
