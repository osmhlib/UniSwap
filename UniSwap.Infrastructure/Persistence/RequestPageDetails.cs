using Microsoft.EntityFrameworkCore;
using UniSwap.Application.Dtos;
using UniSwap.Domain.Enums;

namespace UniSwap.Infrastructure.Persistence;

internal sealed record RequestPageDetails(
    Dictionary<Guid, RequestListing> Listings,
    Dictionary<Guid, RequestParticipant> Participants)
{
    public static async Task<RequestPageDetails> LoadAsync(UniSwapDbContext db, Guid viewerId,
        IEnumerable<Guid> listingIds, IEnumerable<Guid> participantIds, CancellationToken ct)
    {
        var requestedListings = listingIds.Distinct().ToArray();
        var requestedParticipants = participantIds.Distinct().ToArray();

        // Historical cards may show completed titles. Photos follow the existing image endpoint's visibility rules.
        var listingRows = await db.Listings.AsNoTracking()
            .Where(x => requestedListings.Contains(x.Id))
            .Where(x => x.OwnerId == viewerId || x.Status == ListingStatus.Published ||
                x.Status == ListingStatus.Reserved || x.Status == ListingStatus.InExchange ||
                x.Status == ListingStatus.Completed)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Status,
                CoverImageId = x.OwnerId == viewerId || x.Status != ListingStatus.Completed
                    ? db.ListingImages.Where(image => image.ListingId == x.Id)
                        .OrderBy(image => image.Id).Select(image => (Guid?)image.Id).FirstOrDefault()
                    : null
            }).ToListAsync(ct);

        var participants = await db.Users.AsNoTracking()
            .Where(x => requestedParticipants.Contains(x.Id))
            .Select(x => new RequestParticipant(x.Id, x.Name)).ToDictionaryAsync(x => x.Id, ct);

        return new RequestPageDetails(listingRows.ToDictionary(x => x.Id,
            x => new RequestListing(x.Id, x.Title, x.Status.ToString(), x.CoverImageId)), participants);
    }
}
