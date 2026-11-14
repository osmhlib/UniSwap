using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Domain.Enums;

namespace UniSwap.Domain.Entities
{
    public class Listing
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
            Status = ListingStatus.Available;
        }

        public void Reserve()
        {
            if (Status != ListingStatus.Available)
                throw new InvalidOperationException("Listing is not available.");

            Status = ListingStatus.Reserved;
        }

        public void MarkAsExchanged()
        {
            if (Status != ListingStatus.Reserved)
                throw new InvalidOperationException("Listing must be reserved first.");

            Status = ListingStatus.Exchanged;
        }
    }
}
