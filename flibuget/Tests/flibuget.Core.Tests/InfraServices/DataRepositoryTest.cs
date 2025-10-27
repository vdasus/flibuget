using System.Data;
using AutoFixture;
using Dapper;
using flibuget.Core.InfraServices;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace flibuget.Core.Tests.InfraServices;

public class DataRepositoryTests : IDisposable
{
    private readonly IFixture _fixture = new Fixture();
    private readonly InMemoryDbConnectionFactory _connectionFactory = new();
    private readonly ILogger _logger = new LoggerFactory().CreateLogger("TestLogger");

    public void Dispose()
    {
        _connectionFactory?.Dispose();
    }

    #region Basic CRUD Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenFound()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var entity = new TestEntity { Name = "TestName" };
        var id = await repo.InsertAsync(entity);

        var result = await repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be(entity.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);

        var result = await repo.GetByIdAsync(99999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEntities()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var entities = _fixture.CreateMany<TestEntity>(3).ToList();

        foreach (var e in entities)
            await repo.InsertAsync(new TestEntity { Name = e.Name });

        var result = await repo.GetAllAsync();

        result.Should().HaveCountGreaterThanOrEqualTo(3);
        result.Select(x => x.Name).Should().Contain(entities.Select(x => x.Name));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoEntities()
    {
        var connectionFactory = new InMemoryDbConnectionFactory();
        var repo = new TestDataRepository(connectionFactory, null, _logger);

        var result = await repo.GetAllAsync();

        result.Should().BeEmpty();
        connectionFactory.Dispose();
    }

    [Fact]
    public async Task InsertAsync_ReturnsNewId()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var entity = new TestEntity { Name = "InsertTest" };

        var newId = await repo.InsertAsync(entity);

        newId.Should().BeGreaterThan(0);

        var inserted = await repo.GetByIdAsync(newId);
        inserted.Should().NotBeNull();
        inserted!.Name.Should().Be(entity.Name);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsTrue_WhenRowsAffected()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var entity = new TestEntity { Name = "BeforeUpdate" };
        var id = await repo.InsertAsync(entity);

        var updatedEntity = new TestEntity { Id = id, Name = "AfterUpdate" };
        var result = await repo.UpdateAsync(updatedEntity);

        result.Should().BeTrue();

        var fetched = await repo.GetByIdAsync(id);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("AfterUpdate");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenEntityNotFound()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var nonExistentEntity = new TestEntity { Id = 99999, Name = "NonExistent" };

        var result = await repo.UpdateAsync(nonExistentEntity);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenRowsAffected()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var entity = new TestEntity { Name = "ToDelete" };
        var id = await repo.InsertAsync(entity);

        var result = await repo.DeleteAsync(id);

        result.Should().BeTrue();

        var deleted = await repo.GetByIdAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenIdNotFound()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);

        var result = await repo.DeleteAsync(99999);

        result.Should().BeFalse();
    }

    #endregion

    #region UnitOfWork Integration Tests

    [Fact]
    public async Task WithUnitOfWork_ShouldUseTransactionConnection()
    {
        var unitOfWork = new UnitOfWork(_connectionFactory);
        var repo = new TestDataRepository(_connectionFactory, unitOfWork, _logger);
        
        unitOfWork.BeginTransaction();
        var entity = new TestEntity { Name = "UnitOfWorkTest" };

        var id = await repo.InsertAsync(entity);
        var result = await repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("UnitOfWorkTest");
        unitOfWork.Commit();
        unitOfWork.Dispose();
    }

    [Fact]
    public async Task WithUnitOfWork_MultipleOperations_ShouldCommitTogether()
    {
        var unitOfWork = new UnitOfWork(_connectionFactory);
        var repo = new TestDataRepository(_connectionFactory, unitOfWork, _logger);

        unitOfWork.BeginTransaction();

        var id1 = await repo.InsertAsync(new TestEntity { Name = "Entity1" });
        var id2 = await repo.InsertAsync(new TestEntity { Name = "Entity2" });
        await repo.UpdateAsync(new TestEntity { Id = id1, Name = "Entity1Updated" });

        unitOfWork.Commit();

        var e1 = await repo.GetByIdAsync(id1);
        var e2 = await repo.GetByIdAsync(id2);

        e1.Should().NotBeNull();
        e1!.Name.Should().Be("Entity1Updated");
        e2.Should().NotBeNull();
        e2!.Name.Should().Be("Entity2");

        unitOfWork.Dispose();
    }

    [Fact]
    public async Task WithUnitOfWork_Rollback_ShouldRevertChanges()
    {
        var unitOfWork = new UnitOfWork(_connectionFactory);
        var repo = new TestDataRepository(_connectionFactory, unitOfWork, _logger);

        unitOfWork.BeginTransaction();

        var id = await repo.InsertAsync(new TestEntity { Name = "ToRollback" });
        var insertedDuringTransaction = await repo.GetByIdAsync(id);
        insertedDuringTransaction.Should().NotBeNull();

        unitOfWork.Rollback();

        // Create new repo without UnitOfWork to check if data was rolled back
        var repoWithoutUow = new TestDataRepository(_connectionFactory, null, _logger);
        var result = await repoWithoutUow.GetByIdAsync(id);

        result.Should().BeNull();
        unitOfWork.Dispose();
    }

    [Fact]
    public async Task WithoutUnitOfWork_ShouldCreateNewConnection()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var entity = new TestEntity { Name = "NoUnitOfWork" };

        var id = await repo.InsertAsync(entity);

        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WithUnitOfWorkButNoTransaction_ShouldCreateNewConnection()
    {
        var unitOfWork = new UnitOfWork(_connectionFactory);
        var repo = new TestDataRepository(_connectionFactory, unitOfWork, _logger);
      
        // Don't start transaction
        var entity = new TestEntity { Name = "NoTransaction" };

        var id = await repo.InsertAsync(entity);

        id.Should().BeGreaterThan(0);
        unitOfWork.Dispose();
    }

    #endregion

    #region Protected Methods Tests

    [Fact]
    public async Task QueryAsync_ShouldReturnResults()
    {
 var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);
        await repo.InsertAsync(new TestEntity { Name = "QueryTest1" });
      await repo.InsertAsync(new TestEntity { Name = "QueryTest2" });

        var results = await repo.PublicQueryAsync<TestEntity>(
            "SELECT * FROM TestTable WHERE Name LIKE @Pattern",
    new { Pattern = "QueryTest%" });

      results.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteCommand()
    {
        var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);
 var id = await repo.InsertAsync(new TestEntity { Name = "ExecuteTest" });

    var affectedRows = await repo.PublicExecuteAsync(
     "UPDATE TestTable SET Name = @NewName WHERE Id = @Id",
            new { Id = id, NewName = "Updated" });

  affectedRows.Should().Be(1);
        var updated = await repo.GetByIdAsync(id);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task BulkExecuteAsync_ShouldExecuteMultipleCommands()
    {
        var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);
     var entities = new[]
        {
            new { Name = "Bulk1" },
            new { Name = "Bulk2" },
new { Name = "Bulk3" }
};

        var affectedRows = await repo.PublicBulkExecuteAsync(
       "INSERT INTO TestTable (Name) VALUES (@Name)",
          entities);

        affectedRows.Should().Be(3);
    }

    [Fact]
    public async Task BulkExecuteInTransactionAsync_ShouldExecuteInTransaction()
    {
   var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);
      var entities = new[]
        {
            new { Name = "BulkTx1" },
            new { Name = "BulkTx2" }
 };

        var affectedRows = await repo.PublicBulkExecuteInTransactionAsync(
       "INSERT INTO TestTable (Name) VALUES (@Name)",
            entities);

      affectedRows.Should().Be(2);
  
        var all = await repo.GetAllAsync();
        all.Count(x => x.Name.StartsWith("BulkTx")).Should().Be(2);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldCommitOnSuccess()
    {
     var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);

        var result = await repo.PublicExecuteInTransactionAsync(async (conn, txn) =>
        {
   await conn.ExecuteAsync(
     "INSERT INTO TestTable (Name) VALUES (@Name)",
      new { Name = "TxTest" },
           txn);
            return 42;
     });

        result.Should().Be(42);
        var all = await repo.GetAllAsync();
        all.Should().Contain(x => x.Name == "TxTest");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackOnException()
    {
      var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);

        var act = async () => await repo.PublicExecuteInTransactionAsync(async (conn, txn) =>
        {
            await conn.ExecuteAsync(
      "INSERT INTO TestTable (Name) VALUES (@Name)",
  new { Name = "TxFailTest" },
   txn);
          throw new InvalidOperationException("Simulated error");
#pragma warning disable CS0162
            return 0;
#pragma warning restore CS0162
    });

        await act.Should().ThrowAsync<InvalidOperationException>();
 
        var all = await repo.GetAllAsync();
        all.Should().NotContain(x => x.Name == "TxFailTest");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithNullOperation_ShouldThrowArgumentNullException()
 {
var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);

        var act = async () => await repo.PublicExecuteInTransactionAsync<int>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region SQL Identifier Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("123Invalid")]
    [InlineData("Invalid-Name")]
    [InlineData("Invalid;Name")]
    [InlineData("--InvalidName")]
    public void ValidateSqlIdentifier_ThrowsArgumentException_ForInvalidIdentifiers(string? identifier)
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);

 Action act = () => repo.ValidateSqlIdentifier(identifier!, nameof(identifier));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("ValidName")]
    [InlineData("_valid123")]
    [InlineData("valid.name")]
    [InlineData("table_name")]
    [InlineData("Schema.Table")]
  public void ValidateSqlIdentifier_DoesNotThrow_ForValidIdentifiers(string identifier)
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);

        Action act = () => repo.ValidateSqlIdentifier(identifier, nameof(identifier));

        act.Should().NotThrow();
 }

    [Fact]
    public void ValidatedTableName_ShouldCacheValidation()
    {
        var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);

        var name1 = repo.PublicValidatedTableName;
        var name2 = repo.PublicValidatedTableName;

        name1.Should().Be("TestTable");
        name2.Should().Be("TestTable");
    name1.Should().BeSameAs(name2); // Should be cached
    }

    [Fact]
    public void ValidatedIdColumnName_ShouldCacheValidation()
    {
        var repo = new TestDataRepositoryWithProtectedMethods(_connectionFactory, null, _logger);

        var name1 = repo.PublicValidatedIdColumnName;
        var name2 = repo.PublicValidatedIdColumnName;

name1.Should().Be("Id");
    name2.Should().Be("Id");
  name1.Should().BeSameAs(name2); // Should be cached
}

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_ShouldPassThrough()
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var entity = new TestEntity { Name = "CancelTest" };
        var id = await repo.InsertAsync(entity);
        var cts = new CancellationTokenSource();

  var result = await repo.GetByIdAsync(id, cts.Token);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithCancellationToken_ShouldPassThrough()
    {
      var repo = new TestDataRepository(_connectionFactory, null, _logger);
        var cts = new CancellationTokenSource();

        var result = await repo.GetAllAsync(cts.Token);

  result.Should().NotBeNull();
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_WithNullConnectionFactory_ShouldThrowArgumentNullException()
    {
        var act = () => new TestDataRepository(null!, null, _logger);

        act.Should().Throw<ArgumentNullException>()
        .WithParameterName("connectionFactory");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        var act = () => new TestDataRepository(_connectionFactory, null, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldNotThrow()
    {
        var act = () => new TestDataRepository(_connectionFactory, null, _logger);

        act.Should().NotThrow();
 }

    #endregion
}

#region Helpers

/// <summary>
/// Wrapper that prevents disposal of the underlying connection
/// </summary>
public class NonDisposableConnectionWrapper(IDbConnection connection) : IDbConnection
{
#pragma warning disable CS8767 // Nullability of reference types...
    public string ConnectionString
    {
        get => connection.ConnectionString;
        set => connection.ConnectionString = value;
    }
#pragma warning restore CS8767 // Nullability of reference types...

    public int ConnectionTimeout => connection.ConnectionTimeout;
    public string Database => connection.Database;
    public ConnectionState State => connection.State;

    public IDbTransaction BeginTransaction() => connection.BeginTransaction();
    public IDbTransaction BeginTransaction(IsolationLevel il) => connection.BeginTransaction(il);
    public void ChangeDatabase(string databaseName) => connection.ChangeDatabase(databaseName);
    public void Close() { } // Don't close the underlying connection
    public IDbCommand CreateCommand() => connection.CreateCommand();
  public void Open() => connection.Open();
    public void Dispose() { } // Don't dispose the underlying connection
}

public class InMemoryDbConnectionFactory : IDbConnectionFactory, IDisposable
{
    private readonly SqliteConnection _connection;

    public InMemoryDbConnectionFactory()
    {
 _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        CreateTable();
    }

    private void CreateTable()
    {
        var sql = @"CREATE TABLE TestTable (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL
        );";
    _connection.Execute(sql);
    }

    public IDbConnection CreateConnection() => new NonDisposableConnectionWrapper(_connection);

    public Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
     => Task.FromResult<IDbConnection>(new NonDisposableConnectionWrapper(_connection));

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

public class TestDataRepository(IDbConnectionFactory connectionFactory, IUnitOfWork? unitOfWork, ILogger logger)
    : DataRepository<TestEntity>(connectionFactory, unitOfWork, logger)
{
 protected override string TableName => "TestTable";
  protected override string IdColumnName => "Id";

    protected override string GenerateInsertQuery() => "INSERT INTO TestTable (Name) VALUES (@Name); SELECT last_insert_rowid();";
    protected override string GenerateUpdateQuery() => "UPDATE TestTable SET Name = @Name WHERE Id = @Id";
}

/// <summary>
/// Test repository that exposes protected methods for testing
/// </summary>
public class TestDataRepositoryWithProtectedMethods(IDbConnectionFactory connectionFactory, IUnitOfWork? unitOfWork, ILogger logger)
    : TestDataRepository(connectionFactory, unitOfWork, logger)
{
    public Task<IEnumerable<TResult>> PublicQueryAsync<TResult>(string sql, object? param = null, CancellationToken cancellationToken = default)
        => QueryAsync<TResult>(sql, param, cancellationToken);

public Task<int> PublicExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
        => ExecuteAsync(sql, param, cancellationToken);

    public Task<int> PublicBulkExecuteAsync(string sql, IEnumerable<object> entities, CancellationToken cancellationToken = default)
    => BulkExecuteAsync(sql, entities, cancellationToken);

    public Task<int> PublicBulkExecuteInTransactionAsync(string sql, IEnumerable<object> entities, CancellationToken cancellationToken = default)
     => BulkExecuteInTransactionAsync(sql, entities, cancellationToken);

    public Task<TResult> PublicExecuteInTransactionAsync<TResult>(
        Func<IDbConnection, IDbTransaction, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
        => ExecuteInTransactionAsync(operation, cancellationToken);

    public string PublicValidatedTableName => ValidatedTableName;
  public string PublicValidatedIdColumnName => ValidatedIdColumnName;
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion