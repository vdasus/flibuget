using flibuget.Core.Domain.Attributes;

namespace flibuget.Core.Domain.DTO;
public class AudiobookTagDto(
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
    /// <summary>
    /// Author of the audiobook (mapped to Artist tag).
    /// </summary>
    [TagDisplay(Order = 1, DisplayName = "Author", Watermark = "Author name")]
    public string Author { get; set; } = author;

    /// <summary>
    /// Title of the chapter or part.
    /// </summary>
    [TagDisplay(Order = 2, DisplayName = "Title", Watermark = "Book title")]
    public string Title { get; set; } = title;

    /// <summary>
    /// Title of the whole audiobook (Album tag).
    /// </summary>
    [TagDisplay(Order = 3, DisplayName = "Album", Watermark = "Album name")]
    public string Album { get; set; } = album;

    /// <summary>
    /// Number of the chapter or part.
    /// </summary>
    [TagDisplay(Order = 4, DisplayName = "Track Number", Watermark = "Track number")]
    public int? TrackNumber { get; set; } = trackNumber;

    /// <summary>
    /// Year of audiobook release.
    /// </summary>
    [TagDisplay(Order = 5, DisplayName = "Year", Watermark = "2024")]
    public uint? Year { get; set; } = year;

    /// <summary>
    /// Genre of audio content, e.g. "Audiobook".
    /// </summary>
    [TagDisplay(Order = 6, DisplayName = "Genre", Watermark = "Audiobook")]
    public string Genre { get; set; } = genre;

    /// <summary>
    /// Narrator or reader (Album Artist tag).
    /// </summary>
    [TagDisplay(Order = 7, DisplayName = "Narrator", Watermark = "Narrator name")]
    public string Narrator { get; set; } = narrator;

    /// <summary>
    /// Audio producer or sound engineer.
    /// </summary>
    [TagDisplay(Order = 8, DisplayName = "Producer", Watermark = "Producer name")]
    public string Producer { get; set; } = producer;

    /// <summary>
    /// Publisher of the audiobook.
    /// </summary>
    [TagDisplay(Order = 9, DisplayName = "Publisher", Watermark = "Publisher name")]
    public string Publisher { get; set; } = publisher;

    /// <summary>
    /// Copyright information.
    /// </summary>
    [TagDisplay(Order = 10, DisplayName = "Copyright", Watermark = "Copyright information")]
    public string Copyright { get; set; } = copyright;

    /// <summary>
    /// Unique identifier for Audible or other services (optional).
    /// </summary>
    [TagDisplay(Order = 11, DisplayName = "ASIN", Watermark = "Amazon ASIN")]
    public string ASIN { get; set; } = asin;

    /// <summary>
    /// Additional notes (Comment tag).
    /// </summary>
    [TagDisplay(Order = 12, DisplayName = "Comment", Watermark = "Additional notes", IsMultiLine = true)]
    public string Comment { get; set; } = comment;

    /// <summary>
    /// Book description or synopsis (Subtitle/Description tag).
    /// </summary>
    [TagDisplay(Order = 13, DisplayName = "Description", Watermark = "Book description or synopsis", IsMultiLine = true)]
    public string Description { get; set; } = description;

    /// <summary>
    /// URL or path to the cover image.
    /// </summary>
    [TagDisplay(Ignore = true)]
    public string CoverImageUrl { get; set; } = coverImageUrl;
}