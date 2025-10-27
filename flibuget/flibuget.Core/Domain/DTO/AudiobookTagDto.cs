namespace flibuget.Core.Domain.DTO;
public class AudiobookTagDto(
    string author,
    string title,
    string album,
    int? trackNumber,
    int? year,
    string genre,
    string narrator,
    string producer,
    string copyright,
    string publisher,
    string comment,
    string asin,
    string coverImageUrl)
{
    /// <summary>
    /// Author of the audiobook (mapped to Artist tag).
    /// </summary>
    public string Author { get; set; } = author;

    /// <summary>
    /// Title of the chapter or part.
    /// </summary>
    public string Title { get; set; } = title;

    /// <summary>
    /// Title of the whole audiobook (Album tag).
    /// </summary>
    public string Album { get; set; } = album;

    /// <summary>
    /// Number of the chapter or part.
    /// </summary>
    public int? TrackNumber { get; set; } = trackNumber;

    /// <summary>
    /// Year of audiobook release.
    /// </summary>
    public int? Year { get; set; } = year;

    /// <summary>
    /// Genre of audio content, e.g. "Audiobook".
    /// </summary>
    public string Genre { get; set; } = genre;

    /// <summary>
    /// Narrator or reader (Album Artist tag).
    /// </summary>
    public string Narrator { get; set; } = narrator;

    /// <summary>
    /// Audio producer or sound engineer.
    /// </summary>
    public string Producer { get; set; } = producer;

    /// <summary>
    /// Copyright information.
    /// </summary>
    public string Copyright { get; set; } = copyright;

    /// <summary>
    /// Publisher of the audiobook.
    /// </summary>
    public string Publisher { get; set; } = publisher;

    /// <summary>
    /// Additional notes or description (Comment tag).
    /// </summary>
    public string Comment { get; set; } = comment;

    /// <summary>
    /// Unique identifier for Audible or other services (optional).
    /// </summary>
    public string ASIN { get; set; } = asin;

    /// <summary>
    /// URL or path to the cover image.
    /// </summary>
    public string CoverImageUrl { get; set; } = coverImageUrl;
}