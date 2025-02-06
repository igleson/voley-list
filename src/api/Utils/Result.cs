using api.models;

namespace api.Utils;

using System;

public abstract record Result<TValue, TError>
{
    public Result<TAnotherValue, TError> Map<TAnotherValue>(Func<TValue, TAnotherValue> mapper) =>
        this switch
        {
            Success<TValue, TError> (var value) => mapper(value),
            Failure<TValue, TError> (var error) => error
        };

    public Result<TValue, TAnotherError> MapError<TAnotherError>(Func<TError, TAnotherError> mapper) =>
        this switch
        {
            Success<TValue, TError> (var value) => value,
            Failure<TValue, TError> (var error) => mapper(error)
        };

    public Result<TAnotherValue, TError> FlatMap<TAnotherValue>(Func<TValue, Result<TAnotherValue, TError>> mapper) =>
        this switch
        {
            Success<TValue, TError> (var value) => mapper(value),
            Failure<TValue, TError> (var error) => error
        };

    public Result<TValue, TAnotherError>
        FlatMapError<TAnotherError>(Func<TError, Result<TValue, TAnotherError>> mapper) =>
        this switch
        {
            Success<TValue, TError> (var value) => value,
            Failure<TValue, TError> (var error) => mapper(error)
        };

    public static implicit operator Result<TValue, TError>(TValue obj) => new Success<TValue, TError>(obj);
    
    public static implicit operator Result<TValue, TError>(TError ex) => new Failure<TValue, TError>(ex);
}

public record Success<TValue, _>(TValue v) : Result<TValue, _>
{
    public TValue Value { get; set; } = v;
    
    public static implicit operator Success<TValue, _>(TValue obj) => new(obj);

    public void Deconstruct(out TValue v)
    {
        v = Value;
    }
}

public record Failure<TValue, TError>(TError error) : Result<TValue, TError>
{
    public TError Error { get; set; } = error;
    
    public static implicit operator Failure<TValue, TError>(TError error) => new(error);
    public void Deconstruct(out TError e)
    {
        e = Error;
    }
    
}

/// Type used when you need to represent an operation that can error but doesn't return any value
/// Most likely it would be a something like Result<Unit, TError>
public record Unit
{
    public static Unit Value { get; } = new();

    private Unit()
    {
    }
}