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
        if (file.Tag.Pictures is not { Length: > 0 })
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
                coverImageUrl: coverImageTempPath // custom external reference
            );
        var pic = file.Tag.Pictures[0];
        var ext = pic.MimeType?.Contains("png") == true ? ".png" : ".jpg";
        var tempPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), $"cover_{Guid.NewGuid()}{ext}");
        _fileSystem.File.WriteAllBytes(tempPath, pic.Data.Data);
        coverImageTempPath = tempPath;

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
            coverImageUrl: coverImageTempPath // custom external reference
        );
    }

    public void Write(string filePath, AudiobookTagDto tags, bool overwriteExisting = false)
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (string.IsNullOrWhiteSpace(filePath) || !_fileSystem.File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found", filePath); // disambiguate File

        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        // Array-based fields
        if (ShouldSetArray(tags.Author, tag.Performers)) tag.Performers = [tags.Author];
        if (ShouldSetArray(tags.Narrator, tag.AlbumArtists)) tag.AlbumArtists = [tags.Narrator];

        // Handle multiple genres - split by semicolon and trim whitespace
        if (ShouldSetArray(tags.Genre, tag.Genres))
        {
            var genres = tags.Genre
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            tag.Genres = genres;
        }

        if (ShouldSetArray(tags.Producer, tag.Composers)) tag.Composers = [tags.Producer];

        // Scalar string fields
        if (ShouldSet(tags.Title, tag.Title)) tag.Title = tags.Title;
        if (ShouldSet(tags.Album, tag.Album)) tag.Album = tags.Album;
        if (ShouldSet(tags.Comment, tag.Comment)) tag.Comment = tags.Comment;
        if (ShouldSet(tags.Copyright, tag.Copyright)) tag.Copyright = tags.Copyright;
        if (ShouldSet(tags.Publisher, tag.Publisher)) tag.Publisher = tags.Publisher;
        if (ShouldSet(tags.Year.ToString(), tag.Year.ToString())) tag.Year = (uint)tags.Year!;

        // Numeric fields
        if (tags.TrackNumber.HasValue && (overwriteExisting || tag.Track == 0))
            tag.Track = (uint)tags.TrackNumber.Value;
        if (tags.Year.HasValue && (overwriteExisting || tag.Year == 0))
            tag.Year = tags.Year.Value;

        // Handle cover image
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
