using K4os.Compression.LZ4.Streams;
using System.IO;
using K4os.Compression.LZ4;

internal sealed class Lz4Compressor : ICompressor
{
    private readonly LZ4EncoderSettings? _compressionOptions;
    private readonly LZ4DecoderSettings? _decompressionOptions;

    public Lz4Compressor() : this(null, null)
    {
    }

    public Lz4Compressor(LZ4EncoderSettings? options) : this(options, null)
    {
    }

    public Lz4Compressor(LZ4DecoderSettings? options) : this(null, options)
    {
    }

    public Lz4Compressor(LZ4EncoderSettings? compressionOptions, LZ4DecoderSettings? decompressionOptions)
    {
        _compressionOptions = compressionOptions ?? new LZ4EncoderSettings() {CompressionLevel = LZ4Level.L04_HC};
        _decompressionOptions = decompressionOptions;
    }

    public Stream Compress(Stream stream)
    {
        return LZ4Stream.Encode(stream, _compressionOptions);
    }

    public Stream Decompress(Stream stream)
    {
        return LZ4Stream.Decode(stream, _decompressionOptions);
    }

    public string GetFileExtension()
    {
        return ".lz4";
    }
}