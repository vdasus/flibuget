namespace flibuget.Core.InfraServices.AudioTags;

using flibuget.Core.Domain.DTO;

/// <summary>
/// Generic audio tag read/write service for multiple codecs.
/// </summary>
public interface IAudioTagService
{
 /// <summary>
 /// Reads tags from an audio file.
 /// </summary>
 AudiobookTagDto? Read(string filePath);

 /// <summary>
 /// Writes / updates tags in an audio file.
 /// </summary>
 /// <param name="filePath">Target audio file</param>
 /// <param name="tags">Tag data to apply</param>
 /// <param name="overwriteExisting">If true existing tag fields overwritten even when null/empty</param>
 void Write(string filePath, AudiobookTagDto tags, bool overwriteExisting = false);
}
