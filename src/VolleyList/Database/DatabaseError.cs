namespace VolleyList.Database;

public interface IEventListingError;

public record struct ParticipantAlreadyInserted: IEventListingError
{
    public static ParticipantAlreadyInserted Instance { get; } = new();
}

public record struct ParticipantAlreadyRemoved: IEventListingError
{
    public static ParticipantAlreadyRemoved Instance { get; } = new();
}

public record struct ListingAlreadyExists
{
    public static ListingAlreadyExists Instance { get; } = new();
}