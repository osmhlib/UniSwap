using FluentValidation;
using UniSwap.Domain.Enums;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Validation;

public sealed class ModerationReasonValidator : AbstractValidator<ModerationReasonRequest>
{
    public ModerationReasonValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
}

public sealed class ModerationQueueValidator : AbstractValidator<ModerationQueueRequest>
{
    public ModerationQueueValidator()
    {
        Include(new ListingPagingValidator());
        RuleFor(x => x.Status)
            .Must(x => x is ListingStatus.Published or ListingStatus.Hidden or ListingStatus.Rejected)
            .WithMessage("Status must be Published, Hidden or Rejected.");
    }
}
