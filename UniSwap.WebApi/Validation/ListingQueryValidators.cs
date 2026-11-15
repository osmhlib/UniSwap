using FluentValidation;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Validation;

public sealed class ListingPagingValidator : AbstractValidator<ListingPagingRequest>
{
    public ListingPagingValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 10000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class BrowseListingsValidator : AbstractValidator<BrowseListingsRequest>
{
    public BrowseListingsValidator()
    {
        Include(new ListingPagingValidator());
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Condition).MaximumLength(100);
    }
}
