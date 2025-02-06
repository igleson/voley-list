using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using api.models;
using api.Utils;

namespace api.Storage;

public interface IVolleyListStorage
{
    Task<VolleyListingResult> GetVolleyListByNameAsync(string name);
    Task<VolleyListingResult> SaveAsync(VolleyListing listing);

    Task<VolleyListingResult> CreateAsync(VolleyListing listing);

    public async Task<VolleyListingResult> AddPlayerAsync(string name, Player player)
    {
        if (string.IsNullOrEmpty(name))
        {
            return new NameCantBeEmpty(Entities.VolleyList);
        }
        
        if (string.IsNullOrEmpty(player.name))
        {
            return new NameCantBeEmpty(Entities.Player);
        }
        
        var listResult = await GetVolleyListByNameAsync(name);

        listResult = listResult.Map(list => AddPlayerToList(list, player));

        return listResult switch
        {
            VolleyListingSuccess(var volleyList) => await SaveAsync(volleyList),
            _ => listResult
        };
    }

    public async Task<VolleyListingResult> RemovePlayerAsync(string name, int playerPosition, string playerName)
    {
        var listResult = await GetVolleyListByNameAsync(name);

        listResult = listResult
            .FlatMap(list => RemoveFrom(list, list.Players, playerPosition, playerName))
            .Map(RearrangeLists);

        return listResult switch
        {
            VolleyListingSuccess(var volleyList) => await SaveAsync(volleyList),
            _ => listResult
        };
    }

    public async Task<VolleyListingResult> RemoveReservePlayerAsync(string name, int reservePlayerPosition,
        string reserveName)
    {
        var listResult = await GetVolleyListByNameAsync(name);
        listResult = listResult.FlatMap(list =>
            RemoveFrom(list, list.Reserve, reservePlayerPosition, reserveName, Entities.ReservePlayer));
        return listResult switch
        {
            VolleyListingSuccess(var volleyList) => await SaveAsync(volleyList),
            _ => listResult
        };
    }

    public async Task<VolleyListingResult> AddInviteeAsync(string name, Player invitee)
    {
        var listResult = await GetVolleyListByNameAsync(name);
        
        listResult = listResult.FlatMap<VolleyListing>(listing => AddPlayerToList(listing, invitee));

        return listResult switch
        {
            VolleyListingSuccess(var volleyList) => await SaveAsync(volleyList),
            _ => listResult
        };
    }

    public async Task<VolleyListingResult> PromoteAsync(string name)
    {
        var listResult = await GetVolleyListByNameAsync(name);

        listResult = listResult.Map(list =>
        {
            var concat = list.Players.Concat(list.Reserve);
            list.Players = concat.Take(list.Capacity).ToList();
            list.Reserve = concat.Skip(list.Capacity).ToList();
            list.Promoted = true;
            return list;
        });

        return listResult switch
        {
            VolleyListingSuccess(var volleyList) => await SaveAsync(volleyList),
            _ => listResult
        };
    }

    public VolleyListingResult RemoveFrom(VolleyListing listing, List<Player> playerList, int position, string name,
        Entities entitySearched = Entities.Player)
    {
        if (position >= playerList.Count)
        {
            return new NotFoundError(entitySearched);
        }

        var actualPlayer = playerList[position];

        if (actualPlayer.name.Equals(name, StringComparison.InvariantCulture))
        {
            playerList.RemoveAt(position);
            return listing;
        }

        return new NotFoundError(entitySearched);
    }

    private static VolleyListing AddPlayerToList(VolleyListing listing, Player player)
    {
        if(!listing.Promoted && player.isInvitee())
        {
            listing.Reserve.Add(player);
        }
        else if (listing.Players.Count >= listing.Capacity)
        {
            listing.Reserve.Add(player);
        }
        else
        {
            listing.Players.Add(player);
        }

        return listing;
    }

    private static VolleyListing RearrangeLists(VolleyListing listing)
    {
        if (listing.Reserve.Count == 0)
        {
            return listing;
        }
        
        if (listing.Promoted)
        {
            var concat = listing.Players.Concat(listing.Reserve);
            listing.Players = concat.Take(listing.Capacity).ToList();
            listing.Reserve = concat.Skip(listing.Capacity).ToList();
        }
        else
        {
            // listing.Reserve.FindIndex();
        }

        return listing;
    }
}

public class VolleyListStorage(IAmazonS3 s3Client) : IVolleyListStorage
{
    private const string BUCKET_NAME = "opasidfj0as-voley-list";

    private const string LIST_PREFIX = "lists";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public async Task<VolleyListingResult> GetVolleyListByNameAsync(string name)
    {
        try
        {
            var s3Object = await s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = BUCKET_NAME,
                Key = GenerateKey(name),
            });
            return await JsonSerializer.DeserializeAsync<VolleyListing>(s3Object.ResponseStream, SerializerOptions);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError(Entities.VolleyList);
        }
        catch (AmazonS3Exception ex)
        {
            return new StorageError(ex);
        }
        catch (JsonException ex)
        {
            return new SerializationError(ex);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    public async Task<VolleyListingResult> CreateAsync(VolleyListing listing)
    {
        if (listing.Promoted)
        {
            return CannotCreateCoalescedListingError.Instance;
        }

        if (listing.Capacity <= 0)
        {
            return ListingCapacityHasTobeHigherThanZero.Instance;
        }

        if (listing.Capacity > 50)
        {
            return ListingCapacityCanBeHigherThan50.Instance;
        }

        if (listing.Players.Count > listing.Capacity)
        {
            return PlayerCountCantBeHigherThanTheCapacity.Instance;
        }

        if (await FileExistsAsync(listing))
        {
            return ListAlreadyExistsError.Instance;
        }

        return await SaveAsync(listing);
    }

    public async Task<VolleyListingResult> SaveAsync(VolleyListing listing)
    {
        try
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, listing, SerializerOptions);
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = BUCKET_NAME,
                Key = GenerateKey(listing.Name),
                InputStream = stream,
            });

            return listing;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError(Entities.VolleyList);
        }
        catch (AmazonS3Exception ex)
        {
            return new StorageError(ex);
        }
        catch (JsonException ex)
        {
            return new SerializationError(ex);
        }
        catch (Exception ex)
        {
            return new ExceptionError(ex);
        }
    }

    private static string GenerateKey(string name) => $"{LIST_PREFIX}/{name}.json";

    public async Task<bool> FileExistsAsync(VolleyListing listing)
    {
        try
        {
            var metadataResponse = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = BUCKET_NAME,
                Key = GenerateKey(listing.Name)
            });
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound) return false;

            //status wasn't not found, so throw the exception
            throw;
        }

        return true;
    }
}