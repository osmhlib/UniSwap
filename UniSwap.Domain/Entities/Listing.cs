using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Domain.Enums;

namespace UniSwap.Domain.Entities
{
    public class Listing : AggregateRoot
    {
        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public ListingStatus Status { get; private set; }

        public Listing(Guid ownerId, string title, string description)
        {
            Id = Guid.NewGuid();
            OwnerId = ownerId;
            Title = title;
            Description = description;
            Status = ListingStatus.Draft;
            AddDomainEvent(new Events.ListingCreated(Id, OwnerId, Title, DateTime.UtcNow));
        }

        private Listing(Listing source)
        {
            Id = source.Id;
            OwnerId = source.OwnerId;
            Title = source.Title;
            Description = source.Description;
            Status = source.Status;
        }

        public Listing Copy() => new(this);

        public void Publish()
        {
            if (Status != ListingStatus.Draft)
                throw new InvalidOperationException("Only draft listings can be published.");

            Status = ListingStatus.Available;
            AddDomainEvent(new Events.ListingPublished(Id, OwnerId, DateTime.UtcNow));
        }

        public void Reserve(Guid requesterId)
        {
            if (Status != ListingStatus.Available)
                throw new InvalidOperationException("Listing is not available.");

            Status = ListingStatus.Reserved;
            AddDomainEvent(new Events.ReservationConfirmed(Id, requesterId, DateTime.UtcNow));
        }

        public void MarkAsExchanged()
        {
            if (Status != ListingStatus.Reserved)
                throw new InvalidOperationException("Listing must be reserved first.");

            Status = ListingStatus.Exchanged;
        }
    }
}
