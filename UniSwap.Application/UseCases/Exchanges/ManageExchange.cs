using UniSwap.Application.Dtos;
using UniSwap.Application.Common;
using UniSwap.Application.Interfaces;
using UniSwap.Domain.Entities;

namespace UniSwap.Application.UseCases.Exchanges;

public record ExchangeResponse(Guid Id, Guid ListingId, Guid OfferedListingId, Guid RequesterId,
    Guid RecipientId, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc)
{
    public static ExchangeResponse From(ExchangeRequest request) => new(request.Id, request.ListingId,
        request.OfferedListingId, request.RequesterId, request.RecipientId, request.Status.ToString(),
        request.CreatedAtUtc, request.UpdatedAtUtc);
}

public sealed record ExchangeListItem(Guid Id, Guid ListingId, Guid OfferedListingId, Guid RequesterId,
    Guid RecipientId, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc,
    RequestListing? Listing, RequestListing? OfferedListing, RequestParticipant Counterparty, bool CanAccept)
    : ExchangeResponse(Id, ListingId, OfferedListingId, RequesterId, RecipientId, Status, CreatedAtUtc, UpdatedAtUtc);

public interface IExchangeCatalog
{
    Task<IReadOnlyList<ExchangeListItem>> ListAsync(Guid actorId, bool received, int page, int pageSize, CancellationToken ct);
}

public sealed class ManageExchange(IExchangeRequestRepository requests, IListingRepository listings,
    IUnitOfWork unitOfWork, IListingCache cache)
{
    public async Task<ExchangeResponse> RejectAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var request = await RequireRequestAsync(id);
        if (request.RecipientId != actorId) throw new UnauthorizedAccessException();
        request.Reject(actorId);
        return await SaveAsync(request, ct);
    }

    public async Task<ExchangeResponse> CancelAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var request = await RequireRequestAsync(id);
        if (request.RequesterId != actorId) throw new UnauthorizedAccessException();
        request.Cancel(actorId);
        return await SaveAsync(request, ct);
    }

    public async Task<ExchangeResponse> CompleteAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var request = await RequireRequestAsync(id);
        if (request.RequesterId != actorId && request.RecipientId != actorId)
            throw new UnauthorizedAccessException();
        if (request.Status != Domain.Enums.ExchangeStatus.Accepted) throw new RequestConflictException(ConflictCodes.StateConflict);

        var listing = await listings.GetByIdAsync(request.ListingId) ?? throw new ResourceNotFoundException();
        var offered = await listings.GetByIdAsync(request.OfferedListingId) ?? throw new ResourceNotFoundException();
        request.Complete(actorId, listing, offered);
        await listings.UpdateAsync(listing);
        await listings.UpdateAsync(offered);
        var result = await SaveAsync(request, ct);
        await cache.InvalidateAsync(listing.Id);
        await cache.InvalidateAsync(offered.Id);
        return result;
    }

    private async Task<ExchangeResponse> SaveAsync(ExchangeRequest request, CancellationToken ct)
    {
        await requests.UpdateAsync(request);
        await unitOfWork.SaveChangesAsync(ct);
        return ExchangeResponse.From(request);
    }

    private async Task<ExchangeRequest> RequireRequestAsync(Guid id) =>
        await requests.GetByIdAsync(id) ?? throw new ResourceNotFoundException();
}
