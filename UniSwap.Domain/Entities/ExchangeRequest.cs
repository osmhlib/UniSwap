using System;
using System.Collections.Generic;
using System.Text;
using UniSwap.Domain.Enums;

namespace UniSwap.Domain.Entities
{
    public class ExchangeRequest : AggregateRoot
    {
        public Guid Id { get; private set; }
        public Guid ListingId { get; private set; }
        public Guid RequesterId { get; private set; }
        public ExchangeStatus Status { get; private set; }

        public ExchangeRequest(Guid listingId, Guid requesterId)
        {
            Id = Guid.NewGuid();
            ListingId = listingId;
            RequesterId = requesterId;
            Status = ExchangeStatus.Pending;
        }

        private ExchangeRequest(ExchangeRequest source)
        {
            Id = source.Id;
            ListingId = source.ListingId;
            RequesterId = source.RequesterId;
            Status = source.Status;
        }

        public ExchangeRequest Copy() => new(this);

        public void Accept()
        {
            if (Status != ExchangeStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be accepted.");

            Status = ExchangeStatus.Accepted;
            AddDomainEvent(new Events.ExchangeRequestAccepted(
                Id, ListingId, RequesterId, DateTime.UtcNow));
        }

        public void Reject()
        {
            if (Status != ExchangeStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be rejected.");

            Status = ExchangeStatus.Rejected;
        }

        public void Complete()
        {
            if (Status != ExchangeStatus.Accepted)
                throw new InvalidOperationException("Only accepted requests can be completed.");

            Status = ExchangeStatus.Completed;
        }
    }
}
