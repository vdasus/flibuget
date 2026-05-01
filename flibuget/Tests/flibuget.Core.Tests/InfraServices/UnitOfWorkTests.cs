using System.Data;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using flibuget.Core.InfraServices;
using flibuget.Infrastructure.Data;
using FluentAssertions;
using NSubstitute;

namespace flibuget.Core.Tests.InfraServices;

public class UnitOfWorkTests
{
    private readonly IFixture _fixture;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public UnitOfWorkTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _connectionFactory = _fixture.Freeze<IDbConnectionFactory>();
        _connection = _fixture.Freeze<IDbConnection>();
        _transaction = Substitute.For<IDbTransaction>();
        
        _connectionFactory.CreateConnection().Returns(_connection);
        _connection.BeginTransaction().Returns(_transaction);
    }

    [Fact]
    public void Constructor_WhenConnectionFactoryIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new UnitOfWork(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionFactory");
    }

    [Fact]
    public void Constructor_WhenConnectionFactoryIsValid_ShouldCreateInstance()
    {
        // Act
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Assert
        unitOfWork.Should().NotBeNull();
    }

    [Fact]
    public void Connection_WhenAccessedFirstTime_ShouldCreateConnection()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act
        var connection = unitOfWork.Connection;

        // Assert
        connection.Should().BeSameAs(_connection);
        _connectionFactory.Received(1).CreateConnection();
    }

    [Fact]
    public void Connection_WhenAccessedMultipleTimes_ShouldReturnSameConnection()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act
        var connection1 = unitOfWork.Connection;
        var connection2 = unitOfWork.Connection;

        // Assert
        connection1.Should().BeSameAs(connection2);
        _connectionFactory.Received(1).CreateConnection();
    }

    [Fact]
    public void Transaction_WhenNotStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act
        var act = () => unitOfWork.Transaction;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Transaction has not been started. Call BeginTransaction first.");
    }

    [Fact]
    public void BeginTransaction_ShouldCreateTransaction()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act
        unitOfWork.BeginTransaction();

        // Assert
        _connection.Received(1).BeginTransaction();
        unitOfWork.Transaction.Should().BeSameAs(_transaction);
    }

    [Fact]
    public void BeginTransaction_ShouldEnsureConnectionIsCreated()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act
        unitOfWork.BeginTransaction();

        // Assert
        _connectionFactory.Received(1).CreateConnection();
    }

    [Fact]
    public void Commit_WhenTransactionNotStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act
        var act = () => unitOfWork.Commit();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No transaction to commit.");
    }

    [Fact]
    public void Commit_WhenTransactionStarted_ShouldCommitAndDisposeTransaction()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        unitOfWork.BeginTransaction();

        // Act
        unitOfWork.Commit();

        // Assert
        _transaction.Received(1).Commit();
        _transaction.Received(1).Dispose();
    }

    [Fact]
    public void Commit_AfterCommit_TransactionShouldBeNull()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        unitOfWork.BeginTransaction();
        unitOfWork.Commit();

        // Act
        var act = () => unitOfWork.Transaction;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Transaction has not been started. Call BeginTransaction first.");
    }

    [Fact]
    public void Commit_WhenCommitThrowsException_ShouldDisposeTransactionAndRethrow()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        unitOfWork.BeginTransaction();
        var expectedException = new InvalidOperationException("Commit failed");
        _transaction.When(t => t.Commit()).Do(_ => throw expectedException);

        // Act
        var act = () => unitOfWork.Commit();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Commit failed");
        _transaction.Received(1).Dispose();
    }

    [Fact]
    public void Rollback_WhenTransactionNotStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act
        var act = () => unitOfWork.Rollback();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No transaction to rollback.");
    }

    [Fact]
    public void Rollback_WhenTransactionStarted_ShouldRollbackAndDisposeTransaction()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        unitOfWork.BeginTransaction();

        // Act
        unitOfWork.Rollback();

        // Assert
        _transaction.Received(1).Rollback();
        _transaction.Received(1).Dispose();
    }

    [Fact]
    public void Rollback_AfterRollback_TransactionShouldBeNull()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        unitOfWork.BeginTransaction();
        unitOfWork.Rollback();

        // Act
        var act = () => unitOfWork.Transaction;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Transaction has not been started. Call BeginTransaction first.");
    }

    [Fact]
    public void Rollback_WhenRollbackThrowsException_ShouldDisposeTransactionAndRethrow()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        unitOfWork.BeginTransaction();
        var expectedException = new InvalidOperationException("Rollback failed");
        _transaction.When(t => t.Rollback()).Do(_ => throw expectedException);

        // Act
        var act = () => unitOfWork.Rollback();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rollback failed");
        _transaction.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_ShouldDisposeConnectionAndTransaction()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        unitOfWork.BeginTransaction();

        // Act
        unitOfWork.Dispose();

        // Assert
        _transaction.Received(1).Dispose();
        _connection.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_WhenConnectionNotCreated_ShouldNotThrow()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act
        var act = () => unitOfWork.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldDisposeOnlyOnce()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        unitOfWork.BeginTransaction();

        // Act
        unitOfWork.Dispose();
        unitOfWork.Dispose();

        // Assert
        _transaction.Received(1).Dispose();
        _connection.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_WhenTransactionIsNull_ShouldOnlyDisposeConnection()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);
        _ = unitOfWork.Connection; // Create connection without transaction

        // Act
        unitOfWork.Dispose();

        // Assert
        _connection.Received(1).Dispose();
    }

    [Fact]
    public void UnitOfWork_CompleteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act & Assert
        unitOfWork.BeginTransaction();
        unitOfWork.Transaction.Should().BeSameAs(_transaction);
        
        unitOfWork.Commit();
        _transaction.Received(1).Commit();

        unitOfWork.Dispose();
        _connection.Received(1).Dispose();
    }

    [Fact]
    public void UnitOfWork_RollbackWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        var unitOfWork = new UnitOfWork(_connectionFactory);

        // Act & Assert
        unitOfWork.BeginTransaction();
        unitOfWork.Transaction.Should().BeSameAs(_transaction);
        
        unitOfWork.Rollback();
        _transaction.Received(1).Rollback();

        unitOfWork.Dispose();
        _connection.Received(1).Dispose();
    }
}