using UniSwap.Application.Common;
using UniSwap.Application.Dtos;
using UniSwap.Application.Interfaces;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Application.Users;
using UniSwap.Domain.Enums;

namespace UniSwap.Application.Moderation;

public interface IModerationCatalog
{
    Task<ListingPage> ListListingsAsync(ListingStatus status, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<ProfileResponse>> ListUsersAsync(int page, int pageSize, CancellationToken ct);
}

public enum ModerationDecision { Hide, Reject, ReturnToDraft, Remove }

public sealed class ModerateListing(IListingRepository listings, IUnitOfWork unitOfWork, IListingCache cache)
{
    public async Task<ListingDto> ExecuteAsync(Guid id, ModerationDecision decision, string? reason, CancellationToken ct)
    {
        var listing = await listings.GetByIdAsync(id) ?? throw new ResourceNotFoundException();
        switch (decision)
        {
            case ModerationDecision.Hide: listing.Hide(reason!); break;
            case ModerationDecision.Reject: listing.Reject(reason!); break;
            case ModerationDecision.ReturnToDraft: listing.ReturnToDraft(); break;
            case ModerationDecision.Remove: listing.RemoveByModerator(reason!); break;
            default: throw new ArgumentOutOfRangeException(nameof(decision));
        }

        await listings.UpdateAsync(listing);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.InvalidateAsync(id);
        return ListingDto.From(listing);
    }
}
