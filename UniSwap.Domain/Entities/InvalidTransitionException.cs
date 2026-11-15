namespace UniSwap.Domain.Entities;

public sealed class InvalidTransitionException(string message) : InvalidOperationException(message);
