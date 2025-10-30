using flibuget.Core.Domain.DTO;

namespace flibuget.Core.InfraServices.AudioTags;

/// <summary>
/// Implementation based on TagLib# supporting MP3, AAC/MP4 (M4A/M4B), FLAC, WAV (limited) etc.
/// </summary>
public class AudioTagService : IAudioTagService
{
    public AudiobookTagDto? Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return null; // disambiguate File
        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;
        // Map tag fields
        return new AudiobookTagDto(
        author: tag.FirstPerformer ?? string.Empty,
        title: tag.Title ?? string.Empty,
        album: tag.Album ?? string.Empty,
        trackNumber: (int?)tag.Track,
        year: tag.Year == 0 ? null : (int?)tag.Year,
        genre: tag.FirstGenre ?? string.Empty,
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
        if (!File.Exists(filePath)) throw new FileNotFoundException("Audio file not found", filePath); // disambiguate File
        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        // Performers / Author
        if (!string.IsNullOrEmpty(tags.Author))
        {
            if (overwriteExisting || tag.Performers.Length == 0)
                tag.Performers = [tags.Author];
        }

        if (!string.IsNullOrEmpty(tags.Narrator))
        {
            if (overwriteExisting || tag.AlbumArtists.Length == 0)
                tag.AlbumArtists = [tags.Narrator];
        }

        if (!string.IsNullOrEmpty(tags.Genre))
        {
            if (overwriteExisting || tag.Genres.Length == 0)
                tag.Genres = [tags.Genre];
        }

        // Simple scalar fields
        if (!string.IsNullOrEmpty(tags.Title) && (overwriteExisting || string.IsNullOrEmpty(tag.Title))) tag.Title = tags.Title;
        if (!string.IsNullOrEmpty(tags.Album) && (overwriteExisting || string.IsNullOrEmpty(tag.Album))) tag.Album = tags.Album;
        if (tags.TrackNumber.HasValue && (overwriteExisting || tag.Track == 0)) tag.Track = (uint)tags.TrackNumber.Value;
        if (tags.Year.HasValue && (overwriteExisting || tag.Year == 0)) tag.Year = (uint)tags.Year.Value;
        if (!string.IsNullOrEmpty(tags.Comment) && (overwriteExisting || string.IsNullOrEmpty(tag.Comment))) tag.Comment = tags.Comment;
        if (!string.IsNullOrEmpty(tags.Copyright) && (overwriteExisting || string.IsNullOrEmpty(tag.Copyright))) tag.Copyright = tags.Copyright;
        if (!string.IsNullOrEmpty(tags.Publisher) && (overwriteExisting || string.IsNullOrEmpty(tag.Publisher))) tag.Publisher = tags.Publisher;

        // Producer (no direct field; put into Composer or Comment extension)
        if (!string.IsNullOrEmpty(tags.Producer))
        {
            if (overwriteExisting || tag.Composers.Length == 0)
                tag.Composers = [tags.Producer];
        }

        // Cover image from URL not embedded automatically; user can supply actual file later.
        // Persist changes
        file.Save();
        return;

        // Helper local function
        void Set(ref string? current, string value)
        {
            if (overwriteExisting || string.IsNullOrEmpty(current)) current = value;
        }
    }
}
