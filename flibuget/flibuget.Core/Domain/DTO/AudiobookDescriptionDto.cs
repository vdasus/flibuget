using System.Text.Json.Serialization;

namespace flibuget.Core.Domain.DTO;

///<summary>
/// DTO for audiobook description structured response
/// </summary>
public class AudiobookDescriptionDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("cover_link")]
    public string CoverLink { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("themes")]
    public List<string>? Themes { get; set; }

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("narrator")]
    public string Narrator { get; set; } = string.Empty;

    [JsonPropertyName("age_restriction")]
    public string AgeRestriction { get; set; } = string.Empty;

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;
}