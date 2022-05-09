using System.IO;
using ZstdNet;

internal sealed class ZstdCompressor : ICompressor
{
    private readonly CompressionOptions _compressionOptions;
    private readonly DecompressionOptions _decompressionOptions;

    public ZstdCompressor() : this(CompressionOptions.Default, new DecompressionOptions())
    {
    }

    public ZstdCompressor(CompressionOptions options) : this(options, new DecompressionOptions())
    {
    }

    public ZstdCompressor(DecompressionOptions options) : this(CompressionOptions.Default, options)
    {
    }

    public ZstdCompressor(CompressionOptions compressionOptions, DecompressionOptions decompressionOptions)
    {
        _compressionOptions = compressionOptions;
        _decompressionOptions = decompressionOptions;
    }

    public Stream Compress(Stream stream)
    {
        return new CompressionStream(stream, _compressionOptions);
    }

    public Stream Decompress(Stream stream)
    {
        return new DecompressionStream(stream, _decompressionOptions);
    }

    public string GetFileExtension() => ".zstd";
}
