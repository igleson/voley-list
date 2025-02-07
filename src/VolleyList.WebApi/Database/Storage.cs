using System.Data.SQLite;
using Dapper;
using VolleyList.WebApi.Models;
using OneOf;
using OneOf.Types;

namespace VolleyList.WebApi.Database;

public class Storage(DatabaseContext database)
{
    public async Task<OneOf<Listing, ListingAlreadyExists>> CreateListingAsync(Listing listing, CancellationToken token)
    {
        try
        {
            await database.WithConnectionAsync(conn => conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO listing (id, name, max_size, limit_date_for_invitees) VALUES (@Id, @Name, @MaxSize, @LimitDateForInvitees)",
                listing, cancellationToken: token)), token);
        }
        catch (SQLiteException e) when (e.ResultCode is SQLiteErrorCode.Constraint)
        {
            return ListingAlreadyExists.Instance;
        }

        return listing;
    }

    public async Task<OneOf<ListingEvent, ParticipantAlreadyInserted, ParticipantAlreadyRemoved, NotFound>>
        AddListingEventAsync(
            ListingEvent ev, CancellationToken token)
    {
        var currentEventType = await database.WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<ListingEventType?>(
                new CommandDefinition("SELECT type FROM listing_events WHERE name=@Name AND listing_id=@ListingId ORDER BY date DESC LIMIT 1",
                    ev, cancellationToken: token)), token);

        if (currentEventType is null && ev.Type == ListingEventType.Remove) return new NotFound();

        if (currentEventType == ListingEventType.Remove && ev.Type == ListingEventType.Remove) return ParticipantAlreadyRemoved.Instance;

        if (currentEventType == ListingEventType.Add && ev.Type == ListingEventType.Add) return ParticipantAlreadyInserted.Instance;

        await database.WithConnectionAsync(conn => conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO listing_events (name, listing_id, type, participant_type, date) 
            VALUES (@Name, @ListingId, @Type, @ParticipantType, @Date)
            """,
            ev, cancellationToken: token)), token);
        return ev;
    }

    public async Task<OneOf<Listing, NotFound>> ReadListingAsync(string id, CancellationToken token)
    {
        var listing = await database.WithConnectionAsync(conn => conn.QueryFirstOrDefaultAsync<Listing>(
            new CommandDefinition("SELECT * FROM listing WHERE id=@Id",
                new
                {
                    Id = id
                }, cancellationToken: token)), token);

        if (listing == default) return new NotFound();
        return listing;
    }

    public Task<IEnumerable<ListingEvent>> ReadListingEventsAsync(string id, CancellationToken token)
    {
        return database.WithConnectionAsync(conn =>
            conn.QueryAsync<ListingEvent>(
                new CommandDefinition(
                    "SELECT * FROM listing_events WHERE listing_id=@Id",
                    new
                    {
                        Id = id
                    }, cancellationToken: token)), token);
    }
}