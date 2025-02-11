using OneOf;
using OneOf.Types;
using VolleyList.Database;
using VolleyList.Models;

namespace VolleyList.Services;

public class ListingService(Storage storage)
{
    public Task<CreateListingResult> CreateListingAsync(Listing request, CancellationToken token)
    {
        return storage.CreateListingAsync(request, token);
    }

    public Task<OneOf<ListingEvent, ParticipantAlreadyRemoved, ParticipantAlreadyInserted, NotFound>> AddParticipantAsync(string listingId,
        AddParticipantRequest request, CancellationToken token)
    {
        return storage.AddListingEventAsync(new ListingEvent
        {
            Name = request.Name,
            ListingId = listingId,
            Type = ListingEventType.Add,
            ParticipantType = request.IsInvitee ? ParticipantType.Invitee : ParticipantType.Main,
            Date = DateTime.UtcNow
        }, token);
    }

    public Task<OneOf<ListingEvent, ParticipantAlreadyRemoved, ParticipantAlreadyInserted, NotFound>> RemoveParticipantAsync(string listingId,
        string participantId, CancellationToken token)
    {
        return storage.AddListingEventAsync(new ListingEvent
        {
            Name = participantId,
            ListingId = listingId,
            Type = ListingEventType.Remove,
            Date = DateTime.UtcNow
        }, token);
    }

    public async Task<OneOf<ComputedListing, NotFound>> ComputeListingAsync(string listingId, CancellationToken token)
    {
        var listingResult = await storage.ReadListingAsync(listingId, token);

        if (!listingResult.TryPickT0(out var listing, out _))
        {
            return new NotFound();
        }

        var events = (await storage.ReadListingEventsAsync(listingId, token))
            .OrderBy(@event => @event.Date)
            .ToList();

        return CalculateComputedListing(listing, events);
    }


    private static ComputedListing CalculateComputedListing(Listing listing, List<ListingEvent> events)
    {
        var mainList = new List<ListingParticipant>(listing.MaxSize ?? events.Count);
        var reserveList = new List<ListingParticipant>(10);

        var eventsBeforeLimitForInvitees = (listing.LimitDateToRemoveNameAndNotPay.HasValue
            ? events.TakeWhile(@event => @event.Date < listing.LimitDateToRemoveNameAndNotPay)
            : events).ToList();

        var eventsAfterLimitForInvitees = listing.LimitDateToRemoveNameAndNotPay.HasValue
            ? events.SkipWhile(@event => @event.Date < listing.LimitDateToRemoveNameAndNotPay)
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

        if (listing.LimitDateToRemoveNameAndNotPay.HasValue)
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

public readonly record struct AddParticipantRequest(string Name, bool IsInvitee);

public readonly record struct CreateListingRequest(string Name, int? MaxSize, DateTime LimitDateToRemoveNameAndNotPay)
{
    public static implicit operator Listing(CreateListingRequest request) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        MaxSize = request.MaxSize,
        LimitDateToRemoveNameAndNotPay = request.LimitDateToRemoveNameAndNotPay
    };
}