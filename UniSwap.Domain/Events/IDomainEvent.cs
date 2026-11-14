namespace UniSwap.Domain.Events;

/// <summary>
/// A fact that has already happened in the domain.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
