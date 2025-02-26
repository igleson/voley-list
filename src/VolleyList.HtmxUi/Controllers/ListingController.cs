using System.Dynamic;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using VolleyList.Database;
using VolleyList.Models;
using VolleyList.Services;
using VolleyList.Validators;

namespace HtmxUi.Controllers;

public static class HtmxListingController
{
    private const string TextHtmlContentType = "text/html";
    private static readonly object EmptyObject = new { };
    private static readonly object ListNotFoundObject = new { Error = "Lista não encontrada ou muito antiga, por favor confira o ID utilizado." };

    private static readonly IResult ListNotFoundResult = Results.Content(Templates.DisplayListingFormTemplate(ListNotFoundObject), TextHtmlContentType,
        statusCode: StatusCodes.Status404NotFound);

    public static IResult Index()
    {
        return Results.Content(Templates.IndexTemplate(EmptyObject), TextHtmlContentType);
    }

    public static async Task<IResult> CreateListing(
        HttpContext context,
        ListingValidator validator,
        ListingService service,
        [FromForm] string name,
        [FromForm] string maxSize,
        [FromForm] string limitDateToRemoveNameAndNotPay,
        CancellationToken token)
    {
        DateTime? limitDate = null;

        if (!string.IsNullOrEmpty(limitDateToRemoveNameAndNotPay))
            if (DateTime.TryParse(limitDateToRemoveNameAndNotPay, out var date))
            {
                limitDate = date;
            }
            else
            {
                return Results.Content(
                    Templates.CreateListingTemplating(new
                    {
                        NameValue = name,
                        SizeValue = maxSize,
                        DateValue = limitDateToRemoveNameAndNotPay,
                        NameErrorMessage = "Data limit inválida"
                    }),
                    statusCode: 422);
            }

        var listing = new Listing
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            MaxSize = string.IsNullOrEmpty(maxSize) ? null : int.Parse(maxSize),
            LimitDateToRemoveNameAndNotPay = limitDate
        };

        var result = await service.CreateListingAsync(listing, token);

        return result.Match(
            createdListingResult =>
            {
                if (createdListingResult.TryPickT0(out var createdListing, out _))
                {
                    var computed = new ComputedListing { Listing = createdListing };

                    context.Response.Headers["Hx-Push-Url"] = $"/listings/{computed.Listing.Id}";
                    context.Response.Headers["HX-Trigger"] =
                        $$"""{"listing-visited":{"id" : "{{computed.Listing.Id}}", "name" : "{{computed.Listing.Name}}"} }""";

                    return Results.Content(Templates.DisplayListingFormTemplate(new
                    {
                        Computed = computed
                    }), TextHtmlContentType);
                }

                return Results.Content(
                    Templates.CreateListingTemplating(new
                    {
                        NameValue = name,
                        SizeValue = maxSize,
                        DateValue = limitDateToRemoveNameAndNotPay,
                        NameErrorMessage = "Nome já existente"
                    }),
                    statusCode: 422);
            },
            validationErrors =>
            {
                var returnFormData = ValidationErrorsToReturnFormData(name, listing.MaxSize, listing.LimitDateToRemoveNameAndNotPay, validationErrors);

                return Results.Content(
                    Templates.CreateListingTemplating(returnFormData),
                    statusCode: 422);
            }
        );
    }

    public static async Task<IResult> DisplayListing(HttpContext context, ListingService service, [FromRoute] string id, CancellationToken token)
    {
        var computedResult = await service.ComputeListingAsync(id, token);

        if (computedResult.TryPickT0(out var computed, out _))
        {
            context.Response.Headers["HX-Trigger"] =
                $$"""{"listing-visited":{"id" : "{{computed.Listing.Id}}", "name" : "{{computed.Listing.Name}}"} }""";
            return Results.Content(Templates.ComputedListPageTemplate(new
            {
                Computed = computed
            }), TextHtmlContentType);
        }

        return ListNotFoundResult;
    }

    public static async Task<IResult> AddPlayer(ListingService service, [FromRoute] string id, [FromForm] string name, [FromForm] string? isInvitee,
        CancellationToken token)
    {
        var addParticipantResponse = await service.AddParticipantAsync(id, new AddParticipantRequest
        {
            Name = name, IsInvitee = isInvitee is not null
        }, token);

        if (addParticipantResponse.TryPickT0(out _, out var error))
        {
            return await BuildComputedResult(service, id, token);
        }

        return await error.Match<Task<IResult>>
        (
            alreadyRemoved => BuildComputedResult(service, id, token, alreadyRemoved),
            alreadyInserted => BuildComputedResult(service, id, token, alreadyInserted),
            _ => Task.FromResult(ListNotFoundResult)
        );
    }

    public static async Task<IResult> RemovePlayer(ListingService service, [FromRoute] string id, [FromRoute] string playerName, CancellationToken token)
    {
        var removeParticipantAsync = await service.RemoveParticipantAsync(id, playerName, token);

        if (removeParticipantAsync.TryPickT0(out _, out var errors))
        {
            return await BuildComputedResult(service, id, token);
        }

        return await errors.Match
        (
            alreadyRemoved => BuildComputedResult(service, id, token, alreadyRemoved),
            alreadyInserted => BuildComputedResult(service, id, token, alreadyInserted),
            _ => Task.FromResult(ListNotFoundResult)
        );
    }

    private static async Task<IResult> BuildComputedResult(ListingService service, string id, CancellationToken token,
        IEventListingError? eventListingError = null)
    {
        var computeResponse = await service.ComputeListingAsync(id, token);

        if (!computeResponse.TryPickT0(out var computed, out _))
        {
            return ListNotFoundResult;
        }

        var (contextData, statusCode) = eventListingError switch
        {
            null => (new ComputedDataResult { Computed = computed }, StatusCodes.Status200OK),
            ParticipantAlreadyInserted => (
                new ComputedDataResult { Computed = computed, Error = "Jogador com esse nome já adicionado" }, StatusCodes.Status422UnprocessableEntity),
            ParticipantAlreadyRemoved => (new ComputedDataResult { Computed = computed, Error = "Jogador já removido" },
                StatusCodes.Status422UnprocessableEntity),
            _ => (new ComputedDataResult { Computed = computed }, StatusCodes.Status422UnprocessableEntity)
        };

        return Results.Content(Templates.DisplayListingFormTemplate(contextData), TextHtmlContentType, statusCode: statusCode);
    }

    private static dynamic ValidationErrorsToReturnFormData(string name, int? maxSize, DateTime? limitDateToRemoveNameAndNotPay,
        List<ValidationFailure> validationErrors)
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
        return returnFormData;
    }

    public struct ComputedDataResult
    {
        public ComputedListing Computed { get; init; }
        public string? Error { get; init; }
    }
}