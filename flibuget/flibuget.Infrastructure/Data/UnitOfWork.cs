using System.Data;
using flibuget.Core.InfraServices;

namespace flibuget.Infrastructure.Data;

public sealed class UnitOfWork(IDbConnectionFactory connectionFactory) : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public IDbConnection Connection
    {
        get
        {
            _connection ??= _connectionFactory.CreateConnection();
            return _connection;
        }
    }

    public IDbTransaction Transaction => _transaction ?? throw new InvalidOperationException("Transaction has not been started. Call BeginTransaction first.");

    public void BeginTransaction()
    {
        _transaction = Connection.BeginTransaction();
    }

    public void Commit()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to commit.");

        try
        {
            _transaction.Commit();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Rollback()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to rollback.");

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }

        _disposed = true;
    }
}
