using AutoFixture.Xunit2;
using flibuget.Core.Domain.DTO;
using FluentAssertions;
using System.Text.Json;

namespace flibuget.Core.Tests.DTO;

public class AudiobookDescriptionDtoTests
{
    [Theory, AutoData]
    public void Properties_Can_Be_Set_And_Retrieved(
      string title,
        string author,
   string coverLink,
 string description,
      List<string> themes,
        string duration,
        string narrator,
 string ageRestriction,
        string link)
    {
        // Arrange
   var dto = new AudiobookDescriptionDto();

        // Act
        dto.Title = title;
        dto.Author = author;
        dto.CoverLink = coverLink;
        dto.Description = description;
        dto.Themes = themes;
  dto.Duration = duration;
     dto.Narrator = narrator;
 dto.AgeRestriction = ageRestriction;
        dto.Link = link;

        // Assert
        dto.Title.Should().Be(title);
        dto.Author.Should().Be(author);
        dto.CoverLink.Should().Be(coverLink);
        dto.Description.Should().Be(description);
        dto.Themes.Should().BeEquivalentTo(themes);
   dto.Duration.Should().Be(duration);
        dto.Narrator.Should().Be(narrator);
        dto.AgeRestriction.Should().Be(ageRestriction);
        dto.Link.Should().Be(link);
    }

 [Fact]
    public void Default_Constructor_Initializes_With_Default_Values()
    {
        // Act
        var dto = new AudiobookDescriptionDto();

   // Assert
        dto.Title.Should().BeEmpty();
        dto.Author.Should().BeEmpty();
        dto.CoverLink.Should().BeEmpty();
        dto.Description.Should().BeEmpty();
        dto.Themes.Should().BeNull();
        dto.Duration.Should().BeEmpty();
     dto.Narrator.Should().BeEmpty();
        dto.AgeRestriction.Should().BeEmpty();
        dto.Link.Should().BeEmpty();
    }

    [Theory, AutoData]
    public void Serialization_Uses_Correct_JsonPropertyNames(
        string title,
        string author,
        string coverLink,
        string description,
        List<string> themes,
        string duration,
        string narrator,
   string ageRestriction,
     string link)
    {
        // Arrange
        var dto = new AudiobookDescriptionDto
        {
   Title = title,
            Author = author,
    CoverLink = coverLink,
     Description = description,
            Themes = themes,
            Duration = duration,
   Narrator = narrator,
   AgeRestriction = ageRestriction,
       Link = link
   };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<AudiobookDescriptionDto>(json);

        // Assert
     json.Should().Contain("\"title\":");
        json.Should().Contain("\"author\":");
        json.Should().Contain("\"cover_link\":");
        json.Should().Contain("\"description\":");
        json.Should().Contain("\"themes\":");
        json.Should().Contain("\"duration\":");
        json.Should().Contain("\"narrator\":");
        json.Should().Contain("\"age_restriction\":");
        json.Should().Contain("\"link\":");

   deserialized.Should().BeEquivalentTo(dto);
    }

    [Fact]
  public void Deserialization_Handles_Null_Themes()
    {
  // Arrange
var json = @"{
  ""title"": ""Test Title"",
  ""author"": ""Test Author"",
  ""cover_link"": ""http://example.com"",
  ""description"": ""Test Description"",
  ""themes"": null,
  ""duration"": ""5h 30m"",
  ""narrator"": ""Test Narrator"",
  ""age_restriction"": ""16+"",
  ""link"": ""http://example.com/book""
}";

   // Act
      var dto = JsonSerializer.Deserialize<AudiobookDescriptionDto>(json);

     // Assert
      dto.Should().NotBeNull();
        dto!.Themes.Should().BeNull();
  dto.Title.Should().Be("Test Title");
  dto.Author.Should().Be("Test Author");
    }

    [Fact]
    public void Deserialization_Handles_Missing_Themes()
    {
   // Arrange
   var json = @"{
  ""title"": ""Test Title"",
  ""author"": ""Test Author"",
  ""cover_link"": ""http://example.com"",
  ""description"": ""Test Description"",
  ""duration"": ""5h 30m"",
  ""narrator"": ""Test Narrator"",
  ""age_restriction"": ""16+"",
  ""link"": ""http://example.com/book""
}";

        // Act
        var dto = JsonSerializer.Deserialize<AudiobookDescriptionDto>(json);

   // Assert
        dto.Should().NotBeNull();
  dto!.Themes.Should().BeNull();
    }

  [Fact]
public void Serialization_Roundtrip_Preserves_All_Data()
  {
        // Arrange
        var original = new AudiobookDescriptionDto
        {
            Title = "The Great Adventure",
       Author = "John Doe",
    CoverLink = "https://example.com/cover.jpg",
   Description = "An epic tale of adventure",
            Themes = new List<string> { "Adventure", "Fantasy", "Magic" },
      Duration = "12h 45m",
 Narrator = "Jane Smith",
     AgeRestriction = "12+",
         Link = "https://example.com/audiobook"
 };

        // Act
        var json = JsonSerializer.Serialize(original);
     var deserialized = JsonSerializer.Deserialize<AudiobookDescriptionDto>(json);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Themes_Can_Be_Empty_List()
    {
        // Arrange
 var dto = new AudiobookDescriptionDto
        {
          Themes = new List<string>()
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<AudiobookDescriptionDto>(json);

        // Assert
        deserialized!.Themes.Should().NotBeNull();
   deserialized.Themes.Should().BeEmpty();
    }
}
