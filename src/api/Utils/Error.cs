using System.Text.Json;

namespace api.Utils;

public abstract record Error;

public record CannotCreateCoalescedListingError : Error
{
    private CannotCreateCoalescedListingError()
    {
    }

    public static readonly CannotCreateCoalescedListingError Instance = new();
}

public record ListingCapacityHasTobeHigherThanZero : Error
{
    private ListingCapacityHasTobeHigherThanZero()
    {
    }

    public static readonly ListingCapacityHasTobeHigherThanZero Instance = new();
}

public record ListingCapacityCanBeHigherThan50 : Error
{
    private ListingCapacityCanBeHigherThan50()
    {
    }

    public static readonly ListingCapacityCanBeHigherThan50 Instance = new();
}

public record PlayerCountCantBeHigherThanTheCapacity : Error
{
    private PlayerCountCantBeHigherThanTheCapacity()
    {
    }

    public static readonly PlayerCountCantBeHigherThanTheCapacity Instance = new();
}

public record StorageError(Exception ex) : Error;

public record ExceptionError(Exception Ex) : Error;

public record SerializationError(JsonException ex) : Error;

public record NotFoundError(Entities Entity) : Error;

public record NameCantBeEmpty(Entities Entity) : Error;

public record ListAlreadyExistsError : Error
{
    private ListAlreadyExistsError()
    {
    }
    
    public static readonly ListAlreadyExistsError Instance = new();
}

public enum Entities
{
    VolleyList,
    Player,
    ReservePlayer,
}