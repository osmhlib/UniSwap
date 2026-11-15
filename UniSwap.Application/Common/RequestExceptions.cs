namespace UniSwap.Application.Common;

public sealed class ResourceNotFoundException : Exception;
public sealed class InvalidImageUploadException : Exception;

public sealed class RequestConflictException(string code) : Exception
{
    public string Code { get; } = code;
}

public static class ConflictCodes
{
    public const string StateConflict = "state_conflict";
    public const string ConcurrentUpdate = "concurrent_update";
    public const string ReservationAlreadyPending = "reservation_already_pending";
    public const string EmailAlreadyRegistered = "email_already_registered";
    public const string ImageLimitReached = "image_limit_reached";
    public const string ListingUnavailable = "listing_unavailable";
}
