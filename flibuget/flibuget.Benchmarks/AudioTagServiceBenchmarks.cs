using BenchmarkDotNet.Attributes;
using flibuget.Core.InfraServices.AudioTags;

namespace flibuget.Benchmarks;

internal sealed class CPUUsageDiagnoserAttribute : Attribute { } // Stub attribute to satisfy build

[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class AudioTagServiceBenchmarks
{
    private AudioTagService _service = null!;
    private readonly string _author = "Author";
    private readonly string _title = "Title";
    private readonly string _album = "Album";
    private readonly string _narrator = "Narrator";
    private readonly string _genre = "Audiobook";
    private readonly string _producer = "Producer";
    private readonly string _copyright = "Copyright";
    private readonly string _publisher = "Publisher";
    private readonly string _comment = "Comment";
    private readonly string _asin = "ASIN123";
    private readonly string _cover = "http://example.com/cover.jpg";

    [GlobalSetup]
    public void GlobalSetup()
    {
        _service = new AudioTagService();
    }

    private string CreateIsolatedMp3Path()
    {
        string resourceName = "flibuget.Benchmarks.Assets.silent.mp3"; // Adjust if your namespace/path differs
        string path = Path.Combine(Path.GetTempPath(), "bench_" + Guid.NewGuid().ToString("N") + ".mp3");
        using var stream = typeof(AudioTagServiceBenchmarks).Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        using var file = File.Create(path);
        stream.CopyTo(file);
        return path;
    }

    private flibuget.Core.Domain.DTO.AudiobookTagDto CreateDto() => new(
    author: _author,
    title: _title,
    album: _album,
    trackNumber: 1,
    year: 2024,
    genre: _genre,
    narrator: _narrator,
    producer: _producer,
    copyright: _copyright,
    publisher: _publisher,
    comment: _comment,
    asin: _asin,
    coverImageUrl: _cover);

    [Benchmark]
    public void WriteBasicTags()
    {
        var p = CreateIsolatedMp3Path();
        _service.Write(p, CreateDto(), overwriteExisting: true);
        File.Delete(p);
    }

    // Benchmark scenario for optimization: second write with same tags should ideally skip Save()
    [Benchmark]
    public void WriteNoChanges()
    {
        var p = CreateIsolatedMp3Path();
        var dto = CreateDto();
        _service.Write(p, dto, overwriteExisting: true); // initial write
        _service.Write(p, dto, overwriteExisting: false); // no changes expected
        File.Delete(p);
    }

    [Benchmark]
    public flibuget.Core.Domain.DTO.AudiobookTagDto? ReadTags()
    {
        var p = CreateIsolatedMp3Path();
        _service.Write(p, CreateDto(), overwriteExisting: true);
        var dto = _service.Read(p);
        File.Delete(p);
        return dto;
    }
}
