using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using Npgsql;

namespace VolleyList.WebApi.Database;

public interface IDbConnectionProvider
{
    public DbConnection GetDbConnection();
}

public class SupabaseConnectionProvider : IDbConnectionProvider
{
    private readonly NpgsqlConnectionStringBuilder _builder = new NpgsqlConnectionStringBuilder
    {
        Host = Environment.GetEnvironmentVariable("SUPABASE_HOST")!,
        Port = int.Parse(Environment.GetEnvironmentVariable("SUPABASE_PORT")!),
        Username = Environment.GetEnvironmentVariable("SUPABASE_USER")!,
        Password = Environment.GetEnvironmentVariable("SUPABASE_PASSWORD")!,
        Database = Environment.GetEnvironmentVariable("SUPABASE_DATABASE")!,
    };

    public DbConnection GetDbConnection()
    {
        return new NpgsqlConnection(_builder.ConnectionString);
    }
}

public class SqliteConnectionProvider : IDbConnectionProvider
{
    public DbConnection GetDbConnection()
    {
        return new SQLiteConnection("Data Source=Database/data.sqlite");
    }
}

public class DatabaseContext(IDbConnectionProvider dbConnectionProvider)
{
    public async Task<T> WithConnectionAsync<T>(Func<IDbConnection, Task<T>> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);

        await using var conn = dbConnectionProvider.GetDbConnection();

        return await callback(conn);
    }

    public async Task<T> WithTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback,
        IsolationLevel isolationLevel = IsolationLevel.Unspecified,
        CancellationToken cancellationToken = default)
    {
        await using var conn = dbConnectionProvider.GetDbConnection();
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(isolationLevel, cancellationToken);

        var result = await callback(conn, transaction);

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}