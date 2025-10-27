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
    public void ValidateSqlIdentifier_DoesNotThrow_ForValidIdentifiers(string identifier)
    {
        var repo = new TestDataRepository(_connectionFactory, null, _logger);

        Action act = () => repo.ValidateSqlIdentifier(identifier, nameof(identifier));

        act.Should().NotThrow();
    }
}

//TODO make helpers to be shared
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
        var sql = """
                  CREATE TABLE TestTable (
                      Id INTEGER PRIMARY KEY AUTOINCREMENT,
                      Name TEXT NOT NULL
                  );
                  """;
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

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion