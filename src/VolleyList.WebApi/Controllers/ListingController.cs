using Microsoft.AspNetCore.Mvc;
using VolleyList.Services;

namespace VolleyList.WebApi.Controllers;

public static class ListingController
{
    public static async Task<IResult> CreateListingAsync(ListingService service, [FromBody] CreateListingRequest request, CancellationToken token)
    {
        var response = await service.CreateListingAsync(request, token);

        return response.Match(
            Results.Ok,
            exists => Results.BadRequest("listing already exists"),
            empty => Results.BadRequest("Name cant be empty"),
            past => Results.BadRequest("Limit date cant be in the past"),
            sizeLowerThanOne => Results.BadRequest("Size date cant be lower than one"));
    }

    public static async Task<IResult> AddParticipantAsync(ListingService service, [FromRoute] string listingId, [FromBody] AddParticipantRequest request,
        CancellationToken token)
    {
        var response = await service.AddParticipantAsync(listingId, request, token);

        return response.Match(
            Results.Ok,
            inserted => Results.BadRequest("participant already inserted"),
            removed => Results.BadRequest("participant already removed"),
            found => Results.NotFound()
        );
    }

    public static async Task<IResult> RemoveParticipantAsync(ListingService service, [FromRoute] string listingId, [FromRoute] string participantId,
        CancellationToken token)
    {
        var response = await service.RemoveParticipantAsync(listingId, participantId, token);

        return response.Match(
            _ => Results.Ok(),
            inserted => Results.BadRequest("participant already inserted"),
            removed => Results.BadRequest("participant already removed"),
            found => Results.NotFound()
        );
    }

    public static async Task<IResult> ReadListingAsync(ListingService service, [FromRoute] string listingId, CancellationToken token)
    {
        var listingResult = await service.ComputeListingAsync(listingId, token);

        if (!listingResult.TryPickT0(out var calculatedListing, out _))
        {
            return Results.NotFound();
        }

        return Results.Ok(calculatedListing);
    }
}