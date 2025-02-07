namespace VolleyList.WebApi.Database;

public record struct ParticipantAlreadyInserted
{
    public static ParticipantAlreadyInserted Instance { get; } = new();
}

public record struct ParticipantAlreadyRemoved
{
    public static ParticipantAlreadyRemoved Instance { get; } = new();
}

public record struct ListingAlreadyExists
{
    public static ListingAlreadyExists Instance { get; } = new();
}