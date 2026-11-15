using UniSwap.Domain.Enums;
using UniSwap.Domain.Events;

namespace UniSwap.Domain.Entities;

public class ExchangeRequest : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid OfferedListingId { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid RecipientId { get; private set; }
    public ExchangeStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ExchangeRequest() { }

    public ExchangeRequest(Listing listing, Listing offeredListing, Guid requesterId)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(offeredListing);
        if (requesterId == Guid.Empty || requesterId == listing.OwnerId || offeredListing.OwnerId != requesterId || listing.Id == offeredListing.Id)
            throw new ArgumentException("The offered listing must belong to the requester and the target to another user.");
        if (listing.Status != ListingStatus.Published || offeredListing.Status != ListingStatus.Published)
            throw new InvalidTransitionException("Both listings must be published.");
        Id = Guid.NewGuid(); ListingId = listing.Id; OfferedListingId = offeredListing.Id;
        RequesterId = requesterId; RecipientId = listing.OwnerId;
        Status = ExchangeStatus.Pending; CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    private ExchangeRequest(ExchangeRequest source)
    {
        Id = source.Id; ListingId = source.ListingId; OfferedListingId = source.OfferedListingId;
        RequesterId = source.RequesterId; RecipientId = source.RecipientId; Status = source.Status;
        CreatedAtUtc = source.CreatedAtUtc; UpdatedAtUtc = source.UpdatedAtUtc;
    }
    public ExchangeRequest Copy() => new(this);

    public void Accept(Guid actorId, Listing listing, Listing offeredListing)
    {
        RequirePending();
        if (actorId != RecipientId) throw new UnauthorizedAccessException("Only the recipient can accept an offer.");
        ValidateListings(listing, offeredListing, ListingStatus.Published);
        listing.StartExchange(); offeredListing.StartExchange();
        Status = ExchangeStatus.Accepted; UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new ExchangeRequestAccepted(Id, ListingId, RequesterId, UpdatedAtUtc));
    }

    public void Reject(Guid actorId)
    {
        RequirePending();
        if (actorId != RecipientId) throw new UnauthorizedAccessException("Only the recipient can reject an offer.");
        Status = ExchangeStatus.Rejected; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(Guid actorId)
    {
        RequirePending();
        if (actorId != RequesterId) throw new UnauthorizedAccessException("Only the requester can cancel an offer.");
        Status = ExchangeStatus.Cancelled; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Complete(Guid actorId, Listing listing, Listing offeredListing)
    {
        if (Status != ExchangeStatus.Accepted) throw new InvalidTransitionException("Only accepted offers can be completed.");
        if (actorId != RequesterId && actorId != RecipientId) throw new UnauthorizedAccessException("Only participants can complete an exchange.");
        ValidateListings(listing, offeredListing, ListingStatus.InExchange);
        listing.Complete(); offeredListing.Complete();
        Status = ExchangeStatus.Completed; UpdatedAtUtc = DateTime.UtcNow;
    }

    private void RequirePending()
    {
        if (Status != ExchangeStatus.Pending) throw new InvalidTransitionException("Only pending offers can be changed.");
    }

    private void ValidateListings(Listing listing, Listing offeredListing, ListingStatus status)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(offeredListing);
        if (listing.Id != ListingId || offeredListing.Id != OfferedListingId) throw new ArgumentException("Listings do not match the offer.");
        if (listing.Status != status || offeredListing.Status != status) throw new InvalidTransitionException("Both listings must be in the expected status.");
    }
}
