using flibuget.Core.Domain.DTO;
using flibuget.Core.InfraServices.AudioTags;
using System.IO.Abstractions;
using TagLib;

namespace flibuget.Infrastructure.AudioTags;

public class AudioTagService(IFileSystem? fileSystem = null) : IAudioTagService
{
    private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

    public AudiobookTagDto Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !_fileSystem.File.Exists(filePath))
            throw new FileNotFoundException($"Audio file not found [{filePath}]");

        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        var genreString = tag.Genres is { Length: > 0 }
            ? string.Join("; ", tag.Genres)
            : string.Empty;

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
        // TPE2 (AlbumArtist) -> Author, TCOM (Composer) -> Narrator
        // TIT2 (Title) -> Title, TALB (Album) -> Album title
        // For Description, prefer tag.Description (M4B desc) over tag.Subtitle (TIT3)
        var description = !string.IsNullOrEmpty(tag.Description)
            ? tag.Description
            : tag.Subtitle ?? string.Empty;

        return new AudiobookTagDto(
            author: tag.FirstAlbumArtist ?? string.Empty,
            title: tag.Title ?? string.Empty,
            album: tag.Album ?? string.Empty,
            trackNumber: (int?)tag.Track,
            year: tag.Year == 0 ? null : tag.Year,
            genre: genreString,
            narrator: tag.JoinedComposers,
            producer: string.Empty,
            copyright: tag.Copyright ?? string.Empty,
            publisher: tag.Publisher ?? string.Empty,
            comment: tag.Comment ?? string.Empty,
            description: description,
            asin: string.Empty,
            coverImageUrl: coverImageTempPath
        );
    }

    public void Write(string filePath, AudiobookTagDto tags, bool overwriteExisting = false)
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (string.IsNullOrWhiteSpace(filePath) || !_fileSystem.File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found", filePath);

        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;

        if (ShouldSetArray(tags.Author, tag.AlbumArtists))
            tag.AlbumArtists = [tags.Author];

        if (ShouldSetArray(tags.Author, tag.Performers))
            tag.Performers = [tags.Author];

        if (ShouldSetArray(tags.Narrator, tag.Composers))
            tag.Composers = [tags.Narrator];

        if (!string.IsNullOrWhiteSpace(tags.Genre))
        {
            if (overwriteExisting || tag.Genres.Length == 0)
            {
                var genres = tags.Genre
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                tag.Genres = genres;
            }
        }
        else if (overwriteExisting)
        {
            tag.Genres = [];
        }

        if (ShouldSet(tags.Title, tag.Title)) tag.Title = tags.Title;
        if (ShouldSet(tags.Album, tag.Album)) tag.Album = tags.Album;
        if (ShouldSet(tags.Comment, tag.Comment)) tag.Comment = tags.Comment;
        if (ShouldSet(tags.Description, tag.Description)) tag.Description = tags.Description;
        if (ShouldSet(tags.Description, tag.Subtitle)) tag.Subtitle = tags.Description;
        if (ShouldSet(tags.Copyright, tag.Copyright)) tag.Copyright = tags.Copyright;
        if (ShouldSet(tags.Publisher, tag.Publisher)) tag.Publisher = tags.Publisher;

        if (tags.TrackNumber.HasValue && (overwriteExisting || tag.Track == 0))
            tag.Track = (uint)tags.TrackNumber.Value;

        if (tags.Year.HasValue && (overwriteExisting || tag.Year == 0))
            tag.Year = tags.Year.Value;

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
                if (overwriteExisting || file.Tag.Pictures.Length == 0 || !PictureEquals(file.Tag.Pictures[0], picture))
                    file.Tag.Pictures = [picture];
            }
        }
        else if (overwriteExisting)
        {
            file.Tag.Pictures = [];
        }

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
