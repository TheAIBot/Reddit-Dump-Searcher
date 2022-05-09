using System.IO;

internal interface ICompressor
{
    Stream Compress(Stream stream);

    Stream Decompress(Stream stream);
    string GetFileExtension();
}
