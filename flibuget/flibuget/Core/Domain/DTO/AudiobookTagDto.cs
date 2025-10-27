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
    public string Author { get; set; } = author;
    public string Title { get; set; } = title;
    public string Album { get; set; } = album;
    public int? TrackNumber { get; set; } = trackNumber;
    public int? Year { get; set; } = year;
    public string Genre { get; set; } = genre;
    public string Narrator { get; set; } = narrator;
    public string Producer { get; set; } = producer;
    public string Copyright { get; set; } = copyright;
    public string Publisher { get; set; } = publisher;
    public string Comment { get; set; } = comment;
    public string ASIN { get; set; } = asin;
    public string CoverImageUrl { get; set; } = coverImageUrl;
}