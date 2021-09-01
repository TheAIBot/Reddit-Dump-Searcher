using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZstdNet;

namespace PushShift_Dump_Parser
{

    internal class PushShiftDumpReader : DumpReader
    {
        private readonly string FilePath;
        private const int DefaultBufferSize = 1024 * 128;
        private const int MaxBufferSize = DefaultBufferSize * 8;
        public PushShiftDumpReader(string filePath)
        {
            this.FilePath = filePath;
        }

        public async ValueTask ReadUncompressedDumpFile(string[] searchTerms)
        {
            using var fileStream = File.OpenRead(FilePath);
            await ReadDumpFile(fileStream, searchTerms, IncrementStats);
        }

        public async ValueTask ReadCompressedDumpFile(string[] searchTerms)
        {
            await ReadCompressedDumpFile(searchTerms, IncrementStats);
        }

        public async ValueTask ReadCompressedDumpFile(string[] searchTerms, Func<Memory<byte>, bool, ValueTask> commentHandler)
        {
            using var fileStream = File.OpenRead(FilePath);
            using var wafa = new DecompressionStream(fileStream);

            await ReadDumpFile(wafa, searchTerms, commentHandler);
        }

        private async ValueTask ReadDumpFile(Stream binaryFile, string[] searchTerms, Func<Memory<byte>, bool, ValueTask> commentHandler)
        {
            byte[][] searchTermsAsBytes = searchTerms.Select(Encoding.UTF8.GetBytes).ToArray();

            byte[] buffer = new byte[DefaultBufferSize];
            binaryFile.Read(buffer);
            Memory<byte> bufferRemaining = buffer;

            while (true)
            {
                Memory<byte> commentJSon;
                if (!TryGetNextLine(binaryFile, ref buffer, ref bufferRemaining, out commentJSon))
                {
                    break;
                }

                bool foundAllTerms = true;
                foreach (var term in searchTermsAsBytes)
                {
                    if (commentJSon.Span.IndexOf(term) == -1)
                    {
                        foundAllTerms = false;
                        break;
                    }
                }

                await commentHandler(commentJSon, foundAllTerms);
            }

            IsDoneSearching = true;
        }

        private async ValueTask IncrementStats(Memory<byte> commentJSon, bool foundAllTerms)
        {
            if (foundAllTerms)
            {
                LinesWithTerms++;
            }
            LinesSearched++;
        }

        private bool TryGetNextLine(Stream stream, ref byte[] buffer, ref Memory<byte> bufferRemaining, out Memory<byte> commentJSon)
        {
            //See if a whole comment is in the buffer
            if (TryExtractComment(ref bufferRemaining, out commentJSon))
            {
                return true;
            }

            //No whole comment in the buffer so we need to load
            //more of the comment stream into the buffer.

            //First move the partial comment to the start of the buffer
            bufferRemaining.CopyTo(buffer);

            while (true)
            {
                //Then fill out the rest of the buffer with data from the stream
                int bytesRead = stream.Read(buffer, bufferRemaining.Length, buffer.Length - bufferRemaining.Length);

                //If nothing could be read from the stream, then the whole stream
                //has been read and there is no more comments.
                if (bytesRead == 0)
                {
                    if (bufferRemaining.Length != 0)
                    {
                        throw new Exception("Partial comment remaining in buffer when reaching EOF.");
                    }
                    commentJSon = new Memory<byte>();
                    return false;
                }

                bufferRemaining = new Memory<byte>(buffer, 0, bufferRemaining.Length + bytesRead);
                if (TryExtractComment(ref bufferRemaining, out commentJSon))
                {
                    return true;
                }

                //If it still wasn't possible to find a comment then the buffer
                //probably isn't big enough. If the buffer is smaller than a 
                //certain max size then resize the buffer, fill it up and
                //then try again.
                if (buffer.Length >= MaxBufferSize)
                {
                    throw new Exception("Failed to find a whole comment within the buffer size limit.");
                }

                //Resize array and remember to update bufferRemaining as well
                Array.Resize(ref buffer, buffer.Length * 2);
                bufferRemaining = new Memory<byte>(buffer, 0, bufferRemaining.Length);
            }
        }

        private bool TryExtractComment(ref Memory<byte> bufferRemaining, out Memory<byte> commentJSon)
        {
            int newLineIndex = bufferRemaining.Span.IndexOf((byte)'\n');
            if (newLineIndex != -1)
            {
                commentJSon = bufferRemaining.Slice(0, newLineIndex);
                bufferRemaining = bufferRemaining.Slice(newLineIndex + 1);
                return true;
            }

            commentJSon = new Memory<byte>();
            return false;
        }
    }
}
