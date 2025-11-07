using flibuget.Core.Domain.DTO;
using TagLib;
using System.IO.Abstractions;

namespace flibuget.Core.InfraServices.AudioTags;

public class AudioTagService(IFileSystem? fileSystem = null) : IAudioTagService
{
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

    public AudiobookTagDto Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !_fileSystem.File.Exists(filePath))
            throw new FileNotFoundException($"Audio file not found [{filePath}]");

        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        // Join multiple genres with semicolon for display/editing
        var genreString = tag.Genres is { Length: > 0 }
            ? string.Join("; ", tag.Genres)
            : string.Empty;

        // Extract cover image if present
        var coverImageTempPath = string.Empty;
        if (file.Tag.Pictures is { Length: > 0 })
        {
            var pic = file.Tag.Pictures[0];
            var ext = pic.MimeType?.Contains("png") == true ? ".png" : ".jpg";
            var tempPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), $"cover_{Guid.NewGuid()}{ext}");
            _fileSystem.File.WriteAllBytes(tempPath, pic.Data.Data);
            coverImageTempPath = tempPath;
        }

        // Map tag fields according to Audible.com specification:
        // TPE2 (AlbumArtist) -> Author
        // TCOM (Composer) -> Narrator
        // TIT2 (Title) -> Title (chapter title)
        // TALB (Album) -> Album (audiobook title)
        // TPE1 (Artist) -> Author, Narrator (combined)
        // TCON (Genre) -> Genre
        // TYER (Year) -> Copyright Year
        // COMM (Comment) -> Publisher's Summary
        // TIT3 (Subtitle) -> Description/Subtitle
        // TCOP (Copyright) -> Copyright
        // TPUB (Publisher) -> Publisher
        return new AudiobookTagDto(
            author: tag.FirstAlbumArtist ?? string.Empty,  // TPE2 (ALBUMARTIST) = Author
            title: tag.Title ?? string.Empty,               // TIT2 (TITLE) = Chapter Title
            album: tag.Album ?? string.Empty,               // TALB (ALBUM) = Audiobook Title
            trackNumber: (int?)tag.Track,
            year: tag.Year == 0 ? null : tag.Year,          // TYER (YEAR) = Copyright Year
            genre: genreString,                              // TCON (GENRE) = Genre1/Genre2
            narrator: tag.JoinedComposers,                   // TCOM (COMPOSER) = Narrator
            producer: string.Empty,                          // Not in Audible spec
            copyright: tag.Copyright ?? string.Empty,        // TCOP (COPYRIGHT) = Copyright
            publisher: tag.Publisher ?? string.Empty,        // TPUB (PUBLISHER) = Publisher
            comment: tag.Comment ?? string.Empty,            // COMM (COMMENT) = Publisher's Summary
            description: tag.Subtitle ?? string.Empty,       // TIT3 (SUBTITLE) = Subtitle/Description
            asin: string.Empty,                              // ASIN = custom field
            coverImageUrl: coverImageTempPath                // CoverUrl = Album Cover Art
        );
    }

    public void Write(string filePath, AudiobookTagDto tags, bool overwriteExisting = false)
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (string.IsNullOrWhiteSpace(filePath) || !_fileSystem.File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found", filePath);

        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        // Map fields according to Audible.com specification:
        // TPE2 (ALBUMARTIST) = Author
        if (ShouldSetArray(tags.Author, tag.AlbumArtists)) 
            tag.AlbumArtists = [tags.Author];

        // TPE1 (ARTIST) = Author, Narrator (combined)
        // Set to Author if we have it
        if (ShouldSetArray(tags.Author, tag.Performers)) 
            tag.Performers = [tags.Author];

        // TCOM (COMPOSER) = Narrator
        if (ShouldSetArray(tags.Narrator, tag.Composers)) 
            tag.Composers = [tags.Narrator];

        // TCON (GENRE) = Genre1/Genre2
        // Handle multiple genres - split by semicolon and trim whitespace
        if (ShouldSetArray(tags.Genre, tag.Genres))
        {
            var genres = tags.Genre
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            tag.Genres = genres;
        }

        // Scalar string fields
        // TIT2 (TITLE) = Chapter Title
        if (ShouldSet(tags.Title, tag.Title)) 
            tag.Title = tags.Title;
        
        // TALB (ALBUM) = Audiobook Title
        if (ShouldSet(tags.Album, tag.Album)) 
            tag.Album = tags.Album;
        
        // COMM (COMMENT) = Publisher's Summary
        if (ShouldSet(tags.Comment, tag.Comment)) 
            tag.Comment = tags.Comment;
        
        // TIT3 (SUBTITLE) = Subtitle/Description
        if (ShouldSet(tags.Description, tag.Subtitle)) 
            tag.Subtitle = tags.Description;
        
        // TCOP (COPYRIGHT) = Copyright
        if (ShouldSet(tags.Copyright, tag.Copyright)) 
            tag.Copyright = tags.Copyright;
        
        // TPUB (PUBLISHER) = Publisher
        if (ShouldSet(tags.Publisher, tag.Publisher)) 
            tag.Publisher = tags.Publisher;

        // Numeric fields
        if (tags.TrackNumber.HasValue && (overwriteExisting || tag.Track == 0))
            tag.Track = (uint)tags.TrackNumber.Value;
        
        // TYER (YEAR) = Copyright Year
        if (tags.Year.HasValue && (overwriteExisting || tag.Year == 0))
            tag.Year = tags.Year.Value;

        // Handle cover image (CoverUrl)
        if (!string.IsNullOrEmpty(tags.CoverImageUrl))
        {
            if (_fileSystem.File.Exists(tags.CoverImageUrl))
            {
                var bytes = _fileSystem.File.ReadAllBytes(tags.CoverImageUrl);
                var mime = tags.CoverImageUrl.EndsWith(".png") ? "image/png" : "image/jpeg";
                var picture = new Picture
                {
                    Type = PictureType.FrontCover,
                    Description = "Cover",
                    MimeType = mime,
                    Data = new ByteVector(bytes)
                };
                // Replace if different or add if none
                if (overwriteExisting || file.Tag.Pictures.Length == 0 || !PictureEquals(file.Tag.Pictures[0], picture))
                {
                    file.Tag.Pictures = [picture];
                }
            }
        }
        else if (overwriteExisting)
        {
            // Remove cover if requested
            file.Tag.Pictures = [];
        }

        // Persist changes
        file.Save();
        return;

        bool ShouldSet(string? incoming, string? existing) =>
            !string.IsNullOrEmpty(incoming) && (overwriteExisting || string.IsNullOrEmpty(existing));

        bool ShouldSetArray(string? incoming, string[] existing) =>
            !string.IsNullOrEmpty(incoming) && (overwriteExisting || existing.Length == 0);

        bool PictureEquals(IPicture? a, IPicture? b)
        {
            if (a == null || b == null) return false;
            return a.Data.Data.SequenceEqual(b.Data.Data);
        }
    }
}
