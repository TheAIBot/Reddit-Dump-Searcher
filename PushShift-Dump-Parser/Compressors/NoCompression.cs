using System;
using System.IO;

internal class NoCompression : ICompressor
{
    public Stream Compress(Stream stream)
    {
        return stream;
    }

    public Stream Decompress(Stream stream)
    {
        return stream;
    }

    public string GetFileExtension()
    {
        throw new NotImplementedException();
    }
}
