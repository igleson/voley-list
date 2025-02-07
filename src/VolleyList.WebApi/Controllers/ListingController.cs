using Microsoft.AspNetCore.Mvc;
using VolleyList.WebApi.Database;
using VolleyList.WebApi.Models;

namespace VolleyList.WebApi.Controllers;

public static class ListingController
{
    public static async Task<IResult> CreateListingAsync(Storage storage, [FromBody] CreateListingRequest request,
        CancellationToken token)
    {
        var response = await storage.CreateListingAsync(request, token);

        return response.Match(
            Results.Ok,
            exists => Results.BadRequest("listing already exists"));
    }

    public static async Task<IResult> AddParticipantAsync(Storage storage, [FromRoute] string listingId,
        [FromBody] AddParticipantRequest request,
        CancellationToken token)
    {
        var response = await storage.AddListingEventAsync(new ListingEvent
        {
            Name = request.Name,
            ListingId = listingId,
            Type = ListingEventType.Add,
            ParticipantType = request.IsInvitee ? ParticipantType.Invitee : ParticipantType.Main,
            Date = DateTime.UtcNow
        }, token);

        return response.Match(
            Results.Ok,
            inserted => Results.BadRequest("participant already inserted"),
            removed => Results.BadRequest("participant already removed"),
            found => Results.NotFound()
        );
    }

    public static async Task<IResult> RemoveParticipantAsync(Storage storage, [FromRoute] string listingId,
        [FromRoute] string participantId, CancellationToken token)
    {
        var response = await storage.AddListingEventAsync(new ListingEvent
        {
            Name = participantId,
            ListingId = listingId,
            Type = ListingEventType.Remove,
            Date = DateTime.UtcNow
        }, token);

        return response.Match(
            _ => Results.Ok(),
            inserted => Results.BadRequest("participant already inserted"),
            removed => Results.BadRequest("participant already removed"),
            found => Results.NotFound()
        );
    }

    public static async Task<IResult> ReadListingAsync(Storage storage, [FromRoute] string listingId,
        CancellationToken token)
    {
        var listingResult = await storage.ReadListingAsync(listingId, token);

        if (!listingResult.TryPickT0(out var listing, out _))
        {
            return Results.NotFound();
        }

        var events = (await storage.ReadListingEventsAsync(listingId, token))
            .OrderBy(@event => @event.Date)
            .ToList();

        return Results.Ok(CalculateComputedListing(listing, events));
    }

    private static ComputedListing CalculateComputedListing(Listing listing, List<ListingEvent> events)
    {
        var mainList = new List<ListingParticipant>(listing.MaxSize ?? events.Count);
        var reserveList = new List<ListingParticipant>(10);

        var eventsBeforeLimitForInvitees = (listing.LimitDateForInvitees.HasValue
            ? events.TakeWhile(@event => @event.Date < listing.LimitDateForInvitees)
            : events).ToList();

        var eventsAfterLimitForInvitees = listing.LimitDateForInvitees.HasValue
            ? events.SkipWhile(@event => @event.Date < listing.LimitDateForInvitees)
            : events;

        foreach (var ev in eventsBeforeLimitForInvitees)
        {
            if (ev.Type is ListingEventType.Remove)
            {
                RemoveBeforeLimitForInvitees(reserveList, ev, mainList);
            }
            else
            {
                var participant = new ListingParticipant
                {
                    Name = ev.Name,
                    IsInvitee = ev.ParticipantType == ParticipantType.Invitee
                };
                if (!participant.IsInvitee && (!listing.MaxSize.HasValue || mainList.Count < listing.MaxSize))
                {
                    mainList.Add(participant);
                }
                else
                {
                    reserveList.Add(participant);
                }
            }
        }

        var payingParticipantsUpToLimit = new List<ListingParticipant>(mainList);
        var quitters = new Stack<ListingParticipant>();

        foreach (var ev in eventsAfterLimitForInvitees)
        {
            if (ev.Type == ListingEventType.Remove)
            {
                var removed = reserveList.RemoveAll(p => p.Name.Equals(ev.Name));
                if (removed > 0) continue;

                var index = mainList.FindIndex(p => p.Name.Equals(ev.Name));
                var removedParticipant = mainList.ElementAt(index);
                mainList.RemoveAt(index);
                if (payingParticipantsUpToLimit.Any(p => p.Name.Equals(removedParticipant.Name)))
                {
                    quitters.Push(removedParticipant);
                }

                var promoted = reserveList.FirstOrDefault();
                if (promoted == default) continue;
                mainList.Add(promoted);

                reserveList.RemoveAt(0);
            }
            else
            {
                var participant = new ListingParticipant
                {
                    Name = ev.Name,
                    IsInvitee = ev.ParticipantType == ParticipantType.Invitee
                };
                if (!listing.MaxSize.HasValue || mainList.Count < listing.MaxSize)
                {
                    mainList.Add(participant);
                }
                else
                {
                    reserveList.Add(participant);
                }
            }
        }

        var payers = new HashSet<ListingParticipant>(mainList);

        if (listing.LimitDateForInvitees.HasValue)
        {
            var amountOfPeoplePaying = listing.MaxSize ?? int.Max(mainList.Count, payingParticipantsUpToLimit.Count);


            while (payers.Count < amountOfPeoplePaying && quitters.Count > 0)
            {
                payers.Add(quitters.Pop());
            }
        }

        return new ComputedListing
        {
            Listing = listing,
            MainList = mainList,
            ReservesList = reserveList,
            PayingParticipants = payers
        };
    }

    private static void RemoveBeforeLimitForInvitees(List<ListingParticipant> reserveList, ListingEvent ev, List<ListingParticipant> mainList)
    {
        var removed = reserveList.RemoveAll(participant => participant.Name.Equals(ev.Name));
        if (removed > 0)
        {
            return;
        }

        removed = mainList.RemoveAll(participant => participant.Name.Equals(ev.Name));

        if (removed <= 0) return;
        var index = reserveList.FindIndex(p => !p.IsInvitee);
        if (index == -1) return;
        var promoted = reserveList.ElementAt(index);
        reserveList.RemoveAt(index);
        mainList.Add(promoted);
    }
}

public record ComputedListing
{
    public Listing Listing { get; init; }

    public IEnumerable<ListingParticipant> MainList { get; init; } = [];

    public IEnumerable<ListingParticipant> ReservesList { get; init; } = [];

    public IEnumerable<ListingParticipant> PayingParticipants { get; init; } = [];
}

public readonly record struct AddParticipantRequest(string Name, bool IsInvitee);

public readonly record struct CreateListingRequest(string Name, int MaxSize, DateTime Date)
{
    public static implicit operator Listing(CreateListingRequest request) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        MaxSize = request.MaxSize,
        LimitDateForInvitees = request.Date
    };
}