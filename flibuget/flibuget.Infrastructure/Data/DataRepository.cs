using System.Data;
using Dapper;
using flibuget.Core.InfraServices;
using Microsoft.Extensions.Logging;

namespace flibuget.Infrastructure.Data;

public abstract class DataRepository<T>(IDbConnectionFactory connectionFactory, IUnitOfWork? unitOfWork, ILogger logger) : IRepository<T>
    where T : class
{
    protected readonly IDbConnectionFactory ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    protected readonly IUnitOfWork? UnitOfWork = unitOfWork;

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private string? _validatedTableName;
    private string? _validatedIdColumnName;

    protected abstract string TableName { get; }
    protected abstract string IdColumnName { get; }

    protected async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (UnitOfWork != null)
        {
            try
            {
                _ = UnitOfWork.Transaction;
                return UnitOfWork.Connection;
            }
            catch (InvalidOperationException)
            {
                // No active transaction, fall through to create new connection
            }
        }

        return await ConnectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
    }

    protected IDbTransaction? GetTransaction()
    {
        if (UnitOfWork == null) return null;

        try
        {
            return UnitOfWork.Transaction;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    protected string ValidatedTableName
    {
        get
        {
            if (_validatedTableName is not null) return _validatedTableName;
            ValidateSqlIdentifier(TableName, nameof(TableName));
            _validatedTableName = TableName;
            return _validatedTableName;
        }
    }

    protected string ValidatedIdColumnName
    {
        get
        {
            if (_validatedIdColumnName is not null) return _validatedIdColumnName;
            ValidateSqlIdentifier(IdColumnName, nameof(IdColumnName));
            _validatedIdColumnName = IdColumnName;
            return _validatedIdColumnName;
        }
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            var transaction = GetTransaction();
            var shouldDisposeConnection = transaction == null;

            try
            {
                var sql = $"SELECT * FROM {ValidatedTableName} WHERE {ValidatedIdColumnName} = @Id";
                return await connection.QuerySingleOrDefaultAsync<T>(sql, new { Id = id }, transaction).ConfigureAwait(false);
            }
            finally
            {
                if (shouldDisposeConnection) connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityType} by id {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            var transaction = GetTransaction();
            var shouldDisposeConnection = transaction == null;

            try
            {
                var sql = $"SELECT * FROM {ValidatedTableName}";
                return await connection.QueryAsync<T>(sql, transaction: transaction).ConfigureAwait(false);
            }
            finally
            {
                if (shouldDisposeConnection) connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            var transaction = GetTransaction();
            var shouldDisposeConnection = transaction == null;

            try
            {
                var insertQuery = GenerateInsertQuery();
                return await connection.ExecuteScalarAsync<int>(insertQuery, entity, transaction).ConfigureAwait(false);
            }
            finally
            {
                if (shouldDisposeConnection) connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            var transaction = GetTransaction();
            var shouldDisposeConnection = transaction == null;

            try
            {
                var updateQuery = GenerateUpdateQuery();
                var result = await connection.ExecuteAsync(updateQuery, entity, transaction).ConfigureAwait(false);
                return result > 0;
            }
            finally
            {
                if (shouldDisposeConnection) connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            var transaction = GetTransaction();
            var shouldDisposeConnection = transaction == null;

            try
            {
                var sql = $"DELETE FROM {ValidatedTableName} WHERE {ValidatedIdColumnName} = @Id";
                var result = await connection.ExecuteAsync(sql, new { Id = id }, transaction).ConfigureAwait(false);
                return result > 0;
            }
            finally
            {
                if (shouldDisposeConnection) connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityType} with id {Id}", typeof(T).Name, id);
            throw;
        }
    }

    protected Task<int> BulkExecuteInTransactionAsync(string sql, IEnumerable<object> entities, CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync((connection, transaction) =>
            connection.ExecuteAsync(sql, entities, transaction: transaction), cancellationToken);
    }

    protected Task<int> BulkExecuteAsync(string sql, IEnumerable<object> entities, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(sql, entities, cancellationToken);
    }

    protected abstract string GenerateInsertQuery();
    protected abstract string GenerateUpdateQuery();

    protected async Task<IEnumerable<TResult>> QueryAsync<TResult>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var transaction = GetTransaction();
        var shouldDisposeConnection = transaction == null;

        try
        {
            return await connection.QueryAsync<TResult>(sql, param, transaction).ConfigureAwait(false);
        }
        finally
        {
            if (shouldDisposeConnection) connection.Dispose();
        }
    }

    protected async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var transaction = GetTransaction();
        var shouldDisposeConnection = transaction == null;

        try
        {
            return await connection.ExecuteAsync(sql, param, transaction).ConfigureAwait(false);
        }
        finally
        {
            if (shouldDisposeConnection) connection.Dispose();
        }
    }

    protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IDbConnection, IDbTransaction, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var connection = await ConnectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction();

        try
        {
            var result = await operation(connection, transaction).ConfigureAwait(false);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void ValidateSqlIdentifier(string identifier, string paramName)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            !System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_.]*$"))
        {
            throw new ArgumentException($"Invalid SQL identifier: {identifier}", paramName);
        }
    }
}
