namespace UniSwap.WebApi.Dtos;

public sealed record SendExchangeRequestDto(Guid ListingId, Guid OfferedListingId, Guid RequesterId);
public sealed record AcceptExchangeRequestDto(Guid ActorId);
