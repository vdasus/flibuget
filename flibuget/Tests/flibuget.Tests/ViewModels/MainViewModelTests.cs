using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using flibuget.ViewModels;
using flibuget.Core.InfraServices.AudioTags;
using flibuget.Core.DomainServices;
using flibuget.Core.InfraServices.AI;
using flibuget.Core.InfraServices; // Added for IWebService

namespace flibuget.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly IFixture _fixture;
    private readonly ILogger<MainViewModel> _mockLogger;
    private readonly IServiceProvider _mockServiceProvider;
    private readonly IAudioTagService _mockTagService;
    private readonly AudiobookService _audiobookService;
    private readonly IWebService _mockHttpService;

    public MainViewModelTests()
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _mockLogger = Substitute.For<ILogger<MainViewModel>>();
        _mockServiceProvider = Substitute.For<IServiceProvider>();
        _mockTagService = Substitute.For<IAudioTagService>();
   
        // Create AudiobookService with mocked dependencies
        var mockAIProvider = Substitute.For<IAIProvider>();
        var mockAudiobookServiceLogger = Substitute.For<ILogger<AudiobookService>>();
        _audiobookService = new AudiobookService(mockAIProvider, mockAudiobookServiceLogger);
        _mockHttpService = Substitute.For<IWebService>();
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Arrange & Act
        var sut = new MainViewModel(_mockLogger, _mockServiceProvider, _mockTagService, _audiobookService, _mockHttpService);

        // Assert
        sut.Should().NotBeNull();
        sut.AudioFiles.Should().NotBeNull();
        sut.AudioFiles.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldLogInformation_WhenInitialized()
    {
        // Arrange & Act
        var sut = new MainViewModel(_mockLogger, _mockServiceProvider, _mockTagService, _audiobookService, _mockHttpService);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("MainViewModel initialized")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties_WithDefaultValues()
    {
        // Arrange
        var sut = new MainViewModel(_mockLogger, _mockServiceProvider, _mockTagService, _audiobookService, _mockHttpService);

        // Assert
        sut.TagFields.Should().NotBeNull();
        sut.TagFields.Should().NotBeEmpty(); // Should have initialized fields from AudiobookTagDto
        sut.TagFields.Should().AllSatisfy(f => f.Value.Should().BeEmpty());
        sut.ConsoleOutput.Should().BeEmpty();
        sut.CurrentFolderPath.Should().BeEmpty();
    }

    [Fact]
    public void SelectAllCommand_ShouldSelectAllFiles()
    {
        // Arrange
        var sut = new MainViewModel(_mockLogger, _mockServiceProvider, _mockTagService, _audiobookService, _mockHttpService);
        sut.AudioFiles.Add(new flibuget.Models.AudiobookFile("test1.mp3"));
        sut.AudioFiles.Add(new flibuget.Models.AudiobookFile("test2.mp3"));

        // Act
        sut.SelectAllCommand.Execute(null);

        // Assert
        sut.AudioFiles.Should().AllSatisfy(f => f.IsSelected.Should().BeTrue());
    }

    [Fact]
    public void ClearSelectionCommand_ShouldClearAllSelections()
    {
        // Arrange
        var sut = new MainViewModel(_mockLogger, _mockServiceProvider, _mockTagService, _audiobookService, _mockHttpService);
        sut.AudioFiles.Add(new flibuget.Models.AudiobookFile("test1.mp3") { IsSelected = true });
        sut.AudioFiles.Add(new flibuget.Models.AudiobookFile("test2.mp3") { IsSelected = true });

        // Act
        sut.ClearSelectionCommand.Execute(null);

        // Assert
        sut.AudioFiles.Should().AllSatisfy(f => f.IsSelected.Should().BeFalse());
    }
}
