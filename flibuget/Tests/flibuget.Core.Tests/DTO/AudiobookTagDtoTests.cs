using AutoFixture.Xunit2;
using flibuget.Core.Domain.DTO;
using FluentAssertions;

namespace flibuget.Core.Tests.DTO;

public class AudiobookTagDtoTests
{
    [Theory, AutoData]
    public void Constructor_Sets_All_Properties(
        string author,
        string title,
        string album,
        int? trackNumber,
        uint? year,
        string genre,
        string narrator,
        string producer,
        string copyright,
        string publisher,
        string comment,
        string description,
        string asin,
        string coverImageUrl)
    {
        // Act
        var dto = new AudiobookTagDto(author, title, album, trackNumber, year, genre, narrator, producer, copyright, publisher, comment, description, asin, coverImageUrl);

        // Assert
        dto.Author.Should().Be(author);
        dto.Title.Should().Be(title);
        dto.Album.Should().Be(album);
        dto.TrackNumber.Should().Be(trackNumber);
        dto.Year.Should().Be(year);
        dto.Genre.Should().Be(genre);
        dto.Narrator.Should().Be(narrator);
        dto.Producer.Should().Be(producer);
        dto.Copyright.Should().Be(copyright);
        dto.Publisher.Should().Be(publisher);
        dto.Comment.Should().Be(comment);
        dto.Description.Should().Be(description);
        dto.ASIN.Should().Be(asin);
        dto.CoverImageUrl.Should().Be(coverImageUrl);
    }

    [Fact]
    public void Constructor_WithNullYear_SetsYearToNull()
    {
        var dto = new AudiobookTagDto("author", "title", "album", null, null,
            "genre", "narrator", "producer", "copyright", "publisher",
            "comment", "description", "asin", "cover");

        dto.Year.Should().BeNull();
        dto.TrackNumber.Should().BeNull();
    }
}