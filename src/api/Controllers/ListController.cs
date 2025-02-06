using System.Text.Json;
using api.models;
using api.Storage;
using api.Utils;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("/api/v1/listing")]
public class ListingController(IVolleyListStorage storage) : ControllerBase
{
    [HttpGet("/{name}")]
    public async Task<ActionResult<VolleyListing>> GetById([FromRoute] string name)
    {
        var listResult = await storage.GetVolleyListByNameAsync(name);

        return listResult switch
        {
            VolleyListingSuccess(var listing) => listing,
            VolleyListingFailure (NotFoundError _) => NotFound("Listing not found"),
            VolleyListingFailure failure => new ContentResult
            {
                StatusCode = 500,
                Content = JsonSerializer.Serialize(failure),
                ContentType = "application/json"
            }
        };
    }

    [HttpPost]
    public async Task<ActionResult<VolleyListing>> Create([FromBody] VolleyListing listing)
    {
        var saveResult = await storage.CreateAsync(listing);
        return saveResult switch
        {
            VolleyListingSuccess(var volleyList) => volleyList,
            VolleyListingFailure (ListAlreadyExistsError) => BadRequest("Listing already exists"),
            VolleyListingFailure (CannotCreateCoalescedListingError) => BadRequest(
                "Listing cannot be created coalesced"),
            VolleyListingFailure (ListingCapacityHasTobeHigherThanZero) => BadRequest(
                "Listing capacity has to be higher than zero"),
            VolleyListingFailure (ListingCapacityCanBeHigherThan50) => BadRequest(
                "Listing capacity can be higher than 50"),
            VolleyListingFailure (PlayerCountCantBeHigherThanTheCapacity) => BadRequest(
                "Player count cannot be higher than the capacity"),
            VolleyListingFailure failure => new ContentResult
            {
                StatusCode = 500,
                Content = JsonSerializer.Serialize(failure),
                ContentType = "application/json"
            }
        };
    }

    [HttpPut("/{id}/player/")]
    public async Task<ActionResult<VolleyListing>> AddPlayerAsync([FromRoute] string id, [FromBody] Player player)
    {
        var addPlayerResult = await storage.AddPlayerAsync(id, player);

        return addPlayerResult switch
        {
            VolleyListingSuccess(var volleyList) => volleyList,
            VolleyListingFailure (NotFoundError (Entities.VolleyList)) => NotFound("Listing not found"),
            VolleyListingFailure failure => new ContentResult
            {
                StatusCode = 500,
                Content = JsonSerializer.Serialize(failure),
                ContentType = "application/json"
            }
        };
    }

    [HttpDelete("/{id}/player/{playerPosition:int}/{playerName}")]
    public async Task<ActionResult<VolleyListing>> RemovePlayerAsync([FromRoute] string id,
        [FromRoute] int playerPosition,
        [FromRoute] string playerName)
    {
        var removePlayerResult = await storage.RemovePlayerAsync(id, playerPosition, playerName);

        return removePlayerResult switch
        {
            VolleyListingSuccess(var volleyList) => volleyList,
            VolleyListingFailure (NotFoundError (Entities.VolleyList)) => NotFound("Listing not found"),
            VolleyListingFailure (NotFoundError (Entities.Player)) => NotFound("Listing not found"),
            VolleyListingFailure failure => new ContentResult
            {
                StatusCode = 500,
                Content = JsonSerializer.Serialize(failure),
                ContentType = "application/json"
            }
        };
    }

    [HttpDelete("/{id}/reserve/{reservePlayerPosition:int}/{reserveName}")]
    public async Task<ActionResult<VolleyListing>> RemoveReservePlayerAsync([FromRoute] string id,
        [FromRoute] int reservePlayerPosition, [FromRoute] string reserveName)
    {
        var removeReserveResult = await storage.RemoveReservePlayerAsync(id, reservePlayerPosition, reserveName);

        return removeReserveResult switch
        {
            VolleyListingSuccess(var volleyList) => volleyList,
            VolleyListingFailure (NotFoundError (Entities.VolleyList)) => NotFound("Listing not found"),
            VolleyListingFailure (NotFoundError (Entities.Player)) => NotFound("Listing not found"),
            VolleyListingFailure failure => new ContentResult
            {
                StatusCode = 500,
                Content = JsonSerializer.Serialize(failure),
                ContentType = "application/json"
            }
        };
    }

    [HttpPut("/{id}/invitee/")]
    public async Task<ActionResult<VolleyListing>> AddInviteeAsync([FromRoute] string id, [FromBody] Player invitee)
    {
        var addPlayerResult = await storage.AddInviteeAsync(id, invitee);

        return addPlayerResult switch
        {
            VolleyListingSuccess(var volleyList) => volleyList,
            VolleyListingFailure (NotFoundError (Entities.VolleyList)) => NotFound("Listing not found"),
            VolleyListingFailure failure => new ContentResult
            {
                StatusCode = 500,
                Content = JsonSerializer.Serialize(failure),
                ContentType = "application/json"
            }
        };
    }

    [ProducesResponseType(StatusCodes.Status201Created)]
    [HttpPut("/{id}/coalesce")]
    public async Task<ActionResult<VolleyListing>> CoalesceAsync(string id)
    {
        var coalesceResult = await storage.PromoteAsync(id);

        return coalesceResult switch
        {
            VolleyListingSuccess(var volleyList) => volleyList,
            VolleyListingFailure (NotFoundError (Entities.VolleyList)) => NotFound("Listing not found"),
            VolleyListingFailure failure => new ContentResult
            {
                StatusCode = 500,
                Content = JsonSerializer.Serialize(failure),
                ContentType = "application/json"
            }
        };
    }
}