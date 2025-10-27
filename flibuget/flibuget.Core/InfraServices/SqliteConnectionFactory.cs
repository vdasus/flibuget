using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace flibuget.Core.InfraServices;

public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration configuration)
    {
        var dbConfig = configuration.GetSection("Database");
        _connectionString = DecryptConnectionString(dbConfig);

        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Database connection string is empty.");
    }

    private static string DecryptConnectionString(IConfigurationSection dbConfig)
    {
        return dbConfig["ConnectionString"]
               ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}