using System.Data;

namespace flibuget.Core.InfraServices;

/// <summary>
/// Factory for creating database connections
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();

    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}