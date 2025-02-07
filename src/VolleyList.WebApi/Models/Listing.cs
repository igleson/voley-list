namespace VolleyList.WebApi.Models;

public readonly record struct Listing
{
    public string Id { get; init; }

    public string Name { get; init; }

    public int? MaxSize { get; init; }

    public DateTime? LimitDateForInvitees { get; init; }
}

public readonly record struct ListingParticipant
{
    public string Name { get; init; }
    public bool IsInvitee { get; init; }
}

public readonly record struct ListingEvent
{
    public string Name { get; init; }

    public string ListingId { get; init; }

    public ListingEventType Type { get; init; }

    public ParticipantType? ParticipantType { get; init; }

    public DateTime Date { get; init; }
}

public enum ListingEventType
{
    Remove = 0,
    Add = 1
}

public enum ParticipantType
{
    Main = 0,
    Invitee = 1
}