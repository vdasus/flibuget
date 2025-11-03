using System.Text.Json.Serialization;

namespace flibuget.Models;

/// <summary>
/// Represents an audiobook project containing folder path and tag edits
/// </summary>
public class AudiobookProject
{
    [JsonPropertyName("folderPath")]
 public string FolderPath { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("album")]
 public string Album { get; set; } = string.Empty;

  [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("genre")]
    public string Genre { get; set; } = string.Empty;

    [JsonPropertyName("narrator")]
    public string Narrator { get; set; } = string.Empty;

    [JsonPropertyName("producer")]
    public string Producer { get; set; } = string.Empty;

    [JsonPropertyName("copyright")]
    public string Copyright { get; set; } = string.Empty;

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = string.Empty;

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("asin")]
    public string ASIN { get; set; } = string.Empty;

    [JsonPropertyName("coverImagePath")]
    public string CoverImagePath { get; set; } = string.Empty;
}
