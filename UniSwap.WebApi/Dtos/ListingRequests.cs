namespace UniSwap.WebApi.Dtos;

public interface IListingInput
{
    string Title { get; }
    string Description { get; }
    string Category { get; }
    string Condition { get; }
}

public sealed record CreateListingRequest(string Title, string Description, string Category, string Condition) : IListingInput;
public sealed record UpdateListingRequest(string Title, string Description, string Category, string Condition) : IListingInput;
public sealed record CreatedResponse(Guid Id);

public class ListingPagingRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class BrowseListingsRequest : ListingPagingRequest
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public string? Condition { get; init; }
    public bool? Available { get; init; }
    public bool OldestFirst { get; init; }
}
