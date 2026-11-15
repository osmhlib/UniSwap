namespace UniSwap.Application.Dtos;

public sealed record RequestParticipant(Guid Id, string Name);

public sealed record RequestListing(Guid Id, string Title, string Status, Guid? CoverImageId);
