using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using flibuget.ViewModels;

namespace flibuget.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly IFixture _fixture;
    private readonly ILogger<MainViewModel> _mockLogger;
    private readonly IServiceProvider _mockServiceProvider;

    public MainViewModelTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _mockLogger = Substitute.For<ILogger<MainViewModel>>();
        _mockServiceProvider = Substitute.For<IServiceProvider>();
  }

    [Fact]
    public void Constructor_ShouldInitialize_WithCorrectGreeting()
    {
        // Arrange & Act
        var sut = new MainViewModel(_mockLogger, _mockServiceProvider);

        // Assert
        sut.Greeting.Should().Be("Flibuget!");
  }

    [Fact]
    public void Constructor_ShouldLogInformation_WhenInitialized()
    {
// Arrange & Act
 var sut = new MainViewModel(_mockLogger, _mockServiceProvider);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
          Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("MainViewModel initialized")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Greeting_ShouldNotBeNull_OrEmpty()
    {
        // Arrange
        var sut = new MainViewModel(_mockLogger, _mockServiceProvider);

        // Assert
        sut.Greeting.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_ShouldLogDebugMessage_WithGreeting()
    {
        // Arrange & Act
        var sut = new MainViewModel(_mockLogger, _mockServiceProvider);

        // Assert
        _mockLogger.Received(1).Log(
  LogLevel.Debug,
            Arg.Any<EventId>(),
        Arg.Is<object>(o => o.ToString()!.Contains("Greeting message")),
 Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Constructor_ShouldLogEnvironmentInformation()
    {
      // Arrange & Act
      var sut = new MainViewModel(_mockLogger, _mockServiceProvider);

   // Assert
        _mockLogger.Received().Log(
            LogLevel.Information,
 Arg.Any<EventId>(),
      Arg.Is<object>(o => o.ToString()!.Contains("Application started for user")),
  Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
