using System.Dynamic;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using VolleyList.Models;
using VolleyList.Services;
using VolleyList.Validators;

namespace HtmxUi.Controllers;

public static class HtmxListingController
{
    private const string TextHtmlContentType = "text/html";
    private static readonly dynamic EmptyObject = new { };

    public static IResult Index()
    {
        return Results.Content(Templates.IndexTemplate(EmptyObject), TextHtmlContentType);
    }

    public static async Task<IResult> CreateListing(HttpContext context, ListingValidator validator, ListingService service, [FromForm] string name,
        [FromForm] int? maxSize,
        [FromForm] DateTime? limitDateToRemoveNameAndNotPay, CancellationToken token)
    {
        var listing = new Listing
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            MaxSize = maxSize,
            LimitDateToRemoveNameAndNotPay = limitDateToRemoveNameAndNotPay
        };

        var result = await service.CreateListingAsync(listing, token);

        return result.Match(
            createdListingResult =>
            {
                if (createdListingResult.TryPickT0(out var createdListing, out _))
                {
                    var computed = new ComputedListing { Listing = createdListing, };

                    context.Response.Headers["Hx-Push-Url"] = $"/listings/{computed.Listing.Id}";

                    return Results.Content(Templates.DisplayListingFormTemplate(computed), TextHtmlContentType);
                }

                var returnFormData = new
                {
                    NameValue = name,
                    SizeValue = maxSize,
                    DateValue = limitDateToRemoveNameAndNotPay,
                    NameErrorMessage = "Nome já existente"
                };

                return Results.Content(
                    Templates.CreateListingTemplating(returnFormData),
                    statusCode: 422);
            },
            validationErrors =>
            {
                dynamic returnFormData = new ExpandoObject();
                returnFormData.NameValue = name;
                returnFormData.SizeValue = maxSize?.ToString();
                returnFormData.DateValue = limitDateToRemoveNameAndNotPay?.ToString("yyyy-MM-ddThh:mm");

                validationErrors.Select(error => error.CustomState).ToList().ForEach(state =>
                {
                    if (state is not CustomValidationState custom) return;
                    if (custom.NameErrorMessage is not null) returnFormData.NameErrorMessage = custom.NameErrorMessage;

                    if (custom.DateErrorMessage is not null) returnFormData.DateErrorMessage = custom.DateErrorMessage;

                    if (custom.SizeErrorMessage is not null) returnFormData.SizeErrorMessage = custom.SizeErrorMessage;
                });

                return Results.Content(
                    Templates.CreateListingTemplating(returnFormData),
                    statusCode: 422);
            }
        );
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