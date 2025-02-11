namespace VolleyList.Database;

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

public record struct ListingSizeHasToBeHigherThanOne
{
    public static ListingSizeHasToBeHigherThanOne Instance { get; } = new();
}

public record struct ListingNameCantBeEmpty
{
    public static ListingNameCantBeEmpty Instance { get; } = new();
}

public record struct LimitDateToRemoveNameAndNotPayCantBeInThePast
{
    public static LimitDateToRemoveNameAndNotPayCantBeInThePast Instance { get; } = new();
}