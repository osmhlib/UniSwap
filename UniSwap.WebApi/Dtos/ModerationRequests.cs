using UniSwap.Domain.Enums;

namespace UniSwap.WebApi.Dtos;

public sealed record ModerationReasonRequest(string Reason);

public sealed class ModerationQueueRequest : ListingPagingRequest
{
    public ListingStatus Status { get; init; } = ListingStatus.Published;
}
