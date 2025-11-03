using flibuget.Core.Domain.DTO;

namespace flibuget.Core.InfraServices.AudioTags;

// ASAP domain or infra service? think about refactoring later
/// <summary>
/// Implementation based on TagLib# supporting MP3, AAC/MP4 (M4A/M4B), FLAC, WAV (limited) etc.
/// </summary>
public class AudioTagService : IAudioTagService
{
    public AudiobookTagDto? Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException($"Audio file not found [{filePath}]"); // disambiguate File

        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        // Join multiple genres with semicolon for display/editing
        var genreString = tag.Genres != null && tag.Genres.Length > 0
            ? string.Join("; ", tag.Genres)
       : string.Empty;

        // Map tag fields
        return new AudiobookTagDto(
   author: tag.FirstPerformer ?? string.Empty,
   title: tag.Title ?? string.Empty,
        album: tag.Album ?? string.Empty,
        trackNumber: (int?)tag.Track,
        year: tag.Year == 0 ? null : tag.Year,
        genre: genreString,
        narrator: tag.FirstAlbumArtist ?? string.Empty,
        producer: tag.JoinedComposers, // fallback
     copyright: tag.Copyright ?? string.Empty,
        publisher: tag.Publisher ?? string.Empty,
        comment: tag.Comment ?? string.Empty,
        asin: string.Empty, // custom, not standard
  coverImageUrl: string.Empty // custom external reference
        );
    }

    public void Write(string filePath, AudiobookTagDto tags, bool overwriteExisting = false)
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found", filePath); // disambiguate File

        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        // Array-based fields
        if (ShouldSetArray(tags.Author, tag.Performers)) tag.Performers = [tags.Author!];
        if (ShouldSetArray(tags.Narrator, tag.AlbumArtists)) tag.AlbumArtists = [tags.Narrator!];

        // Handle multiple genres - split by semicolon and trim whitespace
        if (ShouldSetArray(tags.Genre, tag.Genres))
        {
            var genres = tags.Genre!
          .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .ToArray();
            tag.Genres = genres;
        }

        if (ShouldSetArray(tags.Producer, tag.Composers)) tag.Composers = [tags.Producer!];

        // Scalar string fields
        if (ShouldSet(tags.Title, tag.Title)) tag.Title = tags.Title!;
        if (ShouldSet(tags.Album, tag.Album)) tag.Album = tags.Album!;
        if (ShouldSet(tags.Comment, tag.Comment)) tag.Comment = tags.Comment!;
        if (ShouldSet(tags.Copyright, tag.Copyright)) tag.Copyright = tags.Copyright!;
        if (ShouldSet(tags.Publisher, tag.Publisher)) tag.Publisher = tags.Publisher!;
        if (ShouldSet(tags.Year.ToString(), tag.Year.ToString())) tag.Year = (uint)tags.Year!;

        // Numeric fields
        if (tags.TrackNumber.HasValue && (overwriteExisting || tag.Track == 0))
            tag.Track = (uint)tags.TrackNumber.Value;
        if (tags.Year.HasValue && (overwriteExisting || tag.Year == 0))
            tag.Year = (uint)tags.Year.Value;

        // Persist changes
        file.Save();
        return;

        bool ShouldSet(string? incoming, string? existing) =>
       !string.IsNullOrEmpty(incoming) && (overwriteExisting || string.IsNullOrEmpty(existing));

        bool ShouldSetArray(string? incoming, string[] existing) =>
                   !string.IsNullOrEmpty(incoming) && (overwriteExisting || existing.Length == 0);
    }
}
