using System.IO.Abstractions;
using flibuget.Core.InfraServices.AudioTags;
using flibuget.Core.Domain.DTO;
using FluentAssertions;
using NSubstitute;
using AutoFixture.Xunit2;

namespace flibuget.Core.Tests.InfraServices.AudioTags;

public class AudioTagServiceTests
{
    [Theory, AutoNSubstituteData]
    public void Read_ThrowsFileNotFound_WhenFileDoesNotExist(
        [Frozen] IFileSystem fileSystem,
        AudioTagService sut)
    {
        fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
        var act = () => sut.Read("nofile.mp3");
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*nofile.mp3*");
    }

    [Theory, AutoNSubstituteData]
    public void Write_ThrowsFileNotFound_WhenFileDoesNotExist(
        [Frozen] IFileSystem fileSystem,
        AudioTagService sut,
        AudiobookTagDto tags)
    {
        fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
        var act = () => sut.Write("nofile.mp3", tags);
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("Audio file not found");
    }

    [Theory, AutoNSubstituteData]
    public void Constructor_UsesProvidedFileSystem(
        IFileSystem fileSystem)
    {
        var sut = new AudioTagService(fileSystem);
        sut.Should().NotBeNull();
    }
}
