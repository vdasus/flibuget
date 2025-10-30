# TagLib (TagLib-Sharp)

Repository: https://github.com/mono/taglib-sharp

TagLib (aka TagLib-sharp) is a .NET platform-independent library (tested on Windows/Linux) for reading and writing metadata in media files, including video, audio, and photo formats. This is a convenient one-stop-shop to present or tag all your media collection, regardless of which format/container these might use. You can read/write the standard or more common tags/properties of a media, or you can also create and retrieve your own custom tags.

## Supported Formats (by file extensions)

Video: mkv, ogv, avi, wmv, asf, mp4 (m4p, m4v), mpeg (mpg, mpe, mpv, mpg, m2v)
Audio: aa, aax, aac, aiff, ape, dsf, flac, m4a, m4b, m4p, mp3, mpc, mpp, ogg, oga, wav, wma, wv, webm
Images: bmp, gif, jpeg, pbm, pgm, ppm, pnm, pcx, png, tiff, dng, svg

## Usage in this project

The library is consumed by `AudioTagService` to read and write audiobook metadata uniformly across multiple codecs.

## Notes
- Provides unified tag access (Title, Album, Performers, Genres, etc.).
- Supports custom tagging strategies when standard fields are absent.
- Cross-platform and actively maintained.
