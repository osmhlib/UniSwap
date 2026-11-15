namespace UniSwap.WebApi.Dtos;

public sealed record CreateListingRequest(Guid OwnerId, string Title, string Description, string Category, string Condition);
public sealed record CreatedResponse(Guid Id);
