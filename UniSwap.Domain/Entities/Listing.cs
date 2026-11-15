using UniSwap.Domain.Enums;
using UniSwap.Domain.Events;

namespace UniSwap.Domain.Entities;

public class Listing : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public string Condition { get; private set; }
    public ListingStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public string? ModerationReason { get; private set; }

    public Listing(Guid ownerId, string title, string description, string category, string condition)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner is required.", nameof(ownerId));
        Validate(title, description, category, condition);
        Id = Guid.NewGuid(); OwnerId = ownerId;
        Title = title.Trim(); Description = description.Trim(); Category = category.Trim(); Condition = condition.Trim();
        Status = ListingStatus.Draft; CreatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new ListingCreated(Id, OwnerId, Title, CreatedAtUtc));
    }

    private Listing(Listing source)
    {
        Id = source.Id; OwnerId = source.OwnerId; Title = source.Title; Description = source.Description;
        Category = source.Category; Condition = source.Condition; Status = source.Status;
        CreatedAtUtc = source.CreatedAtUtc; PublishedAtUtc = source.PublishedAtUtc; ModerationReason = source.ModerationReason;
    }

    public Listing Copy() => new(this);

    public void Update(string title, string description, string category, string condition)
    {
        EnsureEditable();
        Validate(title, description, category, condition);
        Title = title.Trim(); Description = description.Trim(); Category = category.Trim(); Condition = condition.Trim();
    }

    public void EnsureEditable()
    {
        if (Status is ListingStatus.Reserved or ListingStatus.InExchange or ListingStatus.Completed)
            throw new InvalidOperationException("This listing cannot be edited or deleted in its current status.");
    }

    public void Publish()
    {
        if (Status != ListingStatus.Draft) throw new InvalidOperationException("Only draft listings can be published.");
        Status = ListingStatus.Published; PublishedAtUtc = DateTime.UtcNow; ModerationReason = null;
        AddDomainEvent(new ListingPublished(Id, OwnerId, PublishedAtUtc.Value));
    }

    public void Reserve(Guid requesterId)
    {
        if (requesterId == Guid.Empty || requesterId == OwnerId) throw new ArgumentException("A different requester is required.", nameof(requesterId));
        if (Status != ListingStatus.Published) throw new InvalidOperationException("Only published listings can be reserved.");
        Status = ListingStatus.Reserved;
        AddDomainEvent(new ReservationConfirmed(Id, requesterId, DateTime.UtcNow));
    }

    public void StartExchange()
    {
        if (Status != ListingStatus.Published) throw new InvalidOperationException("Only published listings can enter an exchange.");
        Status = ListingStatus.InExchange;
    }

    public void Complete()
    {
        if (Status != ListingStatus.InExchange) throw new InvalidOperationException("Only listings in an exchange can be completed.");
        Status = ListingStatus.Completed;
    }

    public void Hide(string reason) => Moderate(ListingStatus.Hidden, reason);
    public void Reject(string reason) => Moderate(ListingStatus.Rejected, reason);

    public void ReturnToDraft()
    {
        if (Status is not (ListingStatus.Hidden or ListingStatus.Rejected))
            throw new InvalidOperationException("Only hidden or rejected listings can be returned to draft.");
        Status = ListingStatus.Draft; ModerationReason = null;
    }

    private void Moderate(ListingStatus target, string reason)
    {
        if (Status != ListingStatus.Published) throw new InvalidOperationException("Only published listings can be moderated.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A moderation reason is required.", nameof(reason));
        Status = target; ModerationReason = reason.Trim();
    }

    private static void Validate(string title, string description, string category, string condition)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length is < 3 or > 100) throw new ArgumentException("Title must contain 3 to 100 characters.", nameof(title));
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 2000) throw new ArgumentException("Description is required and must contain at most 2000 characters.", nameof(description));
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(condition)) throw new ArgumentException("Condition is required.", nameof(condition));
    }
}
