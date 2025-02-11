using Microsoft.AspNetCore.Mvc;
using VolleyList.Models;
using VolleyList.Services;

namespace HtmxUi.Controllers;

public static class HtmxListingController
{
    private const string TextHtmlContentType = "text/html";
    private static readonly dynamic EmptyObject = new { };

    public static IResult Index()
    {
        return Results.Content(Templates.IndexTemplate(EmptyObject), TextHtmlContentType);
    }

    public static async Task<IResult> CreateListing(HttpContext context, ListingService service, [FromForm] string name, [FromForm] int? maxSize,
        [FromForm] DateTime? limitDateToRemoveNameAndNotPay, CancellationToken token)
    {
        var result = await service.CreateListingAsync(new Listing
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            MaxSize = maxSize,
            LimitDateToRemoveNameAndNotPay = limitDateToRemoveNameAndNotPay
        }, token);
        if (!result.TryPickT0(out var createdListing, out _))
        {
            //DEAL WITH IT
        }

        var computedResult = await service.ComputeListingAsync(createdListing.Id, token);

        if (!computedResult.TryPickT0(out var computed, out _))
        {
            //DEAL WITH IT
        }

        context.Response.Headers["Hx-Push-Url"] = $"/listings/{computed.Listing.Id}";

        return Results.Content(Templates.DisplayListingFormTemplate(computed), TextHtmlContentType);
    }

    public static async Task<IResult> DisplayListing(ListingService service, [FromRoute] string id, CancellationToken token)
    {
        var computedResult = await service.ComputeListingAsync(id, token);

        if (!computedResult.TryPickT0(out var computed, out _))
        {
            //DEAL WITH IT
        }

        return Results.Content(Templates.ComputedListPageTemplate(computed), TextHtmlContentType);
    }

    public static async Task<IResult> AddPlayer(ListingService service, [FromRoute] string id, [FromForm] string name, [FromForm] string? isInvitee,
        CancellationToken token)
    {
        var addParticipantResponse = await service.AddParticipantAsync(id, new AddParticipantRequest
        {
            Name = name, IsInvitee = isInvitee is not null
        }, token);

        if (!addParticipantResponse.TryPickT0(out var response, out _))
        {
            //Deal
        }

        var computeResponse = await service.ComputeListingAsync(id, token);

        if (!computeResponse.TryPickT0(out var compute, out _))
        {
            //deal
        }

        return Results.Content(Templates.DisplayListingFormTemplate(compute), TextHtmlContentType);
    }

    public static async Task<IResult> RemovePlayer(ListingService service, [FromRoute] string id, [FromRoute] string playerName, CancellationToken token)
    {
        var removeParticipantAsync = await service.RemoveParticipantAsync(id, playerName, token);

        if (!removeParticipantAsync.TryPickT0(out var response, out _))
        {
            //Deal
        }

        var computeResponse = await service.ComputeListingAsync(id, token);

        if (!computeResponse.TryPickT0(out var compute, out _))
        {
            //deal
        }

        return Results.Content(Templates.DisplayListingFormTemplate(compute), TextHtmlContentType);
    }
}

public readonly record struct CreateListingRequest([FromForm] string Name, [FromForm] int? MaxSize, [FromForm] DateTime? LimitDateToRemoveNameAndNotPay)
{
    public static implicit operator Listing(CreateListingRequest request) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        MaxSize = request.MaxSize,
        LimitDateToRemoveNameAndNotPay = request.LimitDateToRemoveNameAndNotPay
    };
}