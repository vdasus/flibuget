using AutoFixture;
using AutoFixture.AutoNSubstitute;
using flibuget.Core.InfraServices;
using flibuget.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Data;

namespace flibuget.Core.Tests.InfraServices;

public class SqliteConnectionFactoryTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

    [Fact]
    public void Constructor_WhenConnectionStringIsConfigured_ShouldNotThrow()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns("EncryptedConnectionString");
        _configuration.GetSection("Database").Returns(dbSection);

        // Act
        var act = () => new SqliteConnectionFactory(_configuration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WhenConnectionStringIsNull_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns((string?)null);
        _configuration.GetSection("Database").Returns(dbSection);

        // Act
        var act = () => new SqliteConnectionFactory(_configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Database connection string is not configured.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_WhenConnectionStringIsEmptyOrWhitespace_ShouldThrowInvalidOperationException(string connectionString)
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns(connectionString);
        _configuration.GetSection("Database").Returns(dbSection);

        // Act
        var act = () => new SqliteConnectionFactory(_configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateConnection_ShouldReturnOpenConnection()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns("Data Source=:memory:");
        _configuration.GetSection("Database").Returns(dbSection);
        var factory = new SqliteConnectionFactory(_configuration);

        // Act
        using var connection = factory.CreateConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.State.Should().Be(ConnectionState.Open);
    }

    [Fact]
    public void CreateConnection_ShouldReturnSqliteConnection()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns("Data Source=:memory:");
        _configuration.GetSection("Database").Returns(dbSection);
        var factory = new SqliteConnectionFactory(_configuration);

        // Act
        using var connection = factory.CreateConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeAssignableTo<IDbConnection>();
    }

    [Fact]
    public async Task CreateConnectionAsync_ShouldReturnOpenConnection()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns("Data Source=:memory:");
        _configuration.GetSection("Database").Returns(dbSection);
        var factory = new SqliteConnectionFactory(_configuration);

        // Act
        using var connection = await factory.CreateConnectionAsync();

        // Assert
        connection.Should().NotBeNull();
        connection.State.Should().Be(ConnectionState.Open);
    }

    [Fact]
    public async Task CreateConnectionAsync_ShouldReturnSqliteConnection()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns("Data Source=:memory:");
        _configuration.GetSection("Database").Returns(dbSection);
        var factory = new SqliteConnectionFactory(_configuration);

        // Act
        using var connection = await factory.CreateConnectionAsync();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeAssignableTo<IDbConnection>();
    }

    [Fact]
    public async Task CreateConnectionAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns("Data Source=:memory:");
        _configuration.GetSection("Database").Returns(dbSection);
        var factory = new SqliteConnectionFactory(_configuration);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await factory.CreateConnectionAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void CreateConnection_MultipleCalls_ShouldReturnDifferentInstances()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns("Data Source=:memory:");
        _configuration.GetSection("Database").Returns(dbSection);
        var factory = new SqliteConnectionFactory(_configuration);

        // Act
        using var connection1 = factory.CreateConnection();
        using var connection2 = factory.CreateConnection();

        // Assert
        connection1.Should().NotBeSameAs(connection2);
    }

    [Fact]
    public async Task CreateConnectionAsync_MultipleCalls_ShouldReturnDifferentInstances()
    {
        // Arrange
        var dbSection = Substitute.For<IConfigurationSection>();
        dbSection["ConnectionString"].Returns("Data Source=:memory:");
        _configuration.GetSection("Database").Returns(dbSection);
        var factory = new SqliteConnectionFactory(_configuration);

        // Act
        using var connection1 = await factory.CreateConnectionAsync();
        using var connection2 = await factory.CreateConnectionAsync();

        // Assert
        connection1.Should().NotBeSameAs(connection2);
    }
}