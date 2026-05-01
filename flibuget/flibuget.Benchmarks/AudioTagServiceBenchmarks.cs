using BenchmarkDotNet.Attributes;
using flibuget.Infrastructure.AudioTags;

namespace flibuget.Benchmarks;

// [CPUUsageDiagnoser] stub removed

[MemoryDiagnoser]
public class AudioTagServiceBenchmarks
{
    private AudioTagService _service = null!;

    [Params("Author", "A. N. Other")]
    public string _author { get; set; } = "Author";
    [Params("Title", "Benchmarking Audio")]
    public string _title { get; set; } = "Title";
    [Params("Album", "Test Album")]
    public string _album { get; set; } = "Album";
    [Params("Narrator", "N. Arrator")]
    public string _narrator { get; set; } = "Narrator";
    [Params("Audiobook", "Drama")]
    public string _genre { get; set; } = "Audiobook";
    [Params("Producer", "P. Roducer")]
    public string _producer { get; set; } = "Producer";
    [Params("Copyright", "2024")]
    public string _copyright { get; set; } = "Copyright";
    [Params("Publisher", "Pub Name")]
    public string _publisher { get; set; } = "Publisher";
    [Params("Comment", "Benchmark run")]
    public string _comment { get; set; } = "Comment";
    [Params("ASIN123", "ASIN999")]
    public string _asin { get; set; } = "ASIN123";
    [Params("http://example.com/cover.jpg", "http://example.com/largecover.jpg")]
    public string _cover { get; set; } = "http://example.com/cover.jpg";

    [GlobalSetup]
    public void GlobalSetup()
    {
        _service = new AudioTagService();
    }

    private Core.Domain.DTO.AudiobookTagDto CreateDto() => new(
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
        description: "Benchmark description text",
        asin: _asin,
        coverImageUrl: _cover);

    private Core.Domain.DTO.AudiobookTagDto CreateEmptyDto() => new(
        author: string.Empty,
        title: string.Empty,
        album: string.Empty,
        trackNumber: null,
        year: null,
        genre: string.Empty,
        narrator: string.Empty,
        producer: string.Empty,
        copyright: string.Empty,
        publisher: string.Empty,
        comment: string.Empty,
        description: string.Empty,
        asin: string.Empty,
        coverImageUrl: string.Empty);

    [Benchmark]
    public bool WriteBasicTags()
    {
        Console.WriteLine("Starting WriteBasicTags...");
        var p = CreateIsolatedMp3Path();
        try
        {
            _service.Write(p, CreateDto(), overwriteExisting: true);
            Console.WriteLine("Done WriteBasicTags.");
            return true;
        }
        catch
        {
            Console.WriteLine("Done WriteBasicTags (error).");
            return false;
        }
        finally
        {
            File.Delete(p);
        }
    }

    [Benchmark]
    public bool WriteNoChanges()
    {
        Console.WriteLine("Starting WriteNoChanges...");
        var p = CreateIsolatedMp3Path();
        var dto = CreateDto();
        try
        {
            _service.Write(p, dto, overwriteExisting: true); // initial write
            _service.Write(p, dto, overwriteExisting: false); // no changes expected
            Console.WriteLine("Done WriteNoChanges.");
            return true;
        }
        catch
        {
            Console.WriteLine("Done WriteNoChanges (error).");
            return false;
        }
        finally
        {
            File.Delete(p);
        }
    }

    [Benchmark]
    public Core.Domain.DTO.AudiobookTagDto? ReadTags()
    {
        Console.WriteLine("Starting ReadTags...");
        var p = CreateIsolatedMp3Path();
        try
        {
            _service.Write(p, CreateDto(), overwriteExisting: true);
            var result = _service.Read(p);
            Console.WriteLine("Done ReadTags.");
            return result;
        }
        catch
        {
            Console.WriteLine("Done ReadTags (error).");
            return null;
        }
        finally
        {
            File.Delete(p);
        }
    }

    [Benchmark]
    public bool WriteEmptyTags()
    {
        Console.WriteLine("Starting WriteEmptyTags...");
        var p = CreateIsolatedMp3Path();
        try
        {
            _service.Write(p, CreateEmptyDto(), overwriteExisting: true);
            Console.WriteLine("Done WriteEmptyTags.");
            return true;
        }
        catch
        {
            Console.WriteLine("Done WriteEmptyTags (error).");
            return false;
        }
        finally
        {
            File.Delete(p);
        }
    }

    private string CreateIsolatedMp3Path()
    {
        var resourceName = "flibuget.Benchmarks.Assets.silent.mp3"; // Adjust if your namespace/path differs
        var path = Path.Combine(Path.GetTempPath(), "bench_" + Guid.NewGuid().ToString("N") + ".mp3");
        using var stream = typeof(AudioTagServiceBenchmarks).Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        using var file = File.Create(path);
        stream.CopyTo(file);
        return path;
    }
}
