using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PushShift_Dump_Parser
{
    internal static class CompressorHelper
    {
        public static void CompressFolder(string folderPath, ICompressor currentCompression, ICompressor compressTo)
        {
            Parallel.ForEach(Directory.GetFiles(folderPath), fileName =>
            {
                if (fileName.Contains(compressTo.GetFileExtension()))
                {
                    return;
                }

                if (File.Exists(fileName + compressTo.GetFileExtension()))
                {
                    return;
                }

                Console.WriteLine($"Starting to compress: {fileName}");
                CompressDump(fileName, fileName + compressTo.GetFileExtension(), currentCompression, compressTo);
                Console.WriteLine($"Finished to compressing: {fileName}");
            });
        }

        public static void ChangeCompressionLevel(string folderPath, ICompressor from, ICompressor to)
        {
            Parallel.ForEach(Directory.GetFiles(folderPath), fileName =>
            {
                string dstFileName = fileName + "-temp";

                using (var srcFile = File.OpenRead(fileName))
                using (var decompressor = from.Decompress(srcFile))
                using (var dstFile = File.OpenWrite(dstFileName))
                using (var compressor = to.Compress(dstFile))
                {
                    decompressor.CopyTo(compressor);
                }

                File.Delete(fileName);
                File.Move(dstFileName, fileName);
            });
        }

        public static Task SplitFilesIntoSmallerCompressedFiles(string srcDir, string dstDir, ICompressor srcCompression, ICompressor dstCompression)
        {
            return SplitFilesIntoSmallerCompressedFiles(Directory.GetFiles(srcDir), dstDir, srcCompression, dstCompression);
        }

        public static async Task SplitFilesIntoSmallerCompressedFiles(string[] srcFiles, string dstDir, ICompressor srcCompression, ICompressor dstCompression)
        {
            Directory.CreateDirectory(dstDir);
            ActionBlock<string> actionExecutor = new ActionBlock<string>(async srcFileName =>
            {
                string baseDstFileName = Path.Combine(dstDir, Path.GetFileNameWithoutExtension(srcFileName));
                string fileExtension = Path.GetExtension(srcFileName);
                long maxFileSize = 1024 * 1024 * 1024 * 6L;

                var commentSearcher = new PushShiftDumpReader(srcFileName);
                using var commentSplitter = new CommentsIntoChunks(baseDstFileName, fileExtension, maxFileSize, dstCompression);

                await commentSearcher.ReadDumpFile(Array.Empty<string>(), srcCompression, commentSplitter.HandleComment);
            },
            new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                MaxMessagesPerTask = 1
            });

            foreach (var file in srcFiles)
            {
                actionExecutor.Post(file);
            }
            actionExecutor.Complete();
            await actionExecutor.Completion;
        }

        public static void CompressDump(string dumpPath, string compressedDumpPath, ICompressor currentCompression, ICompressor compressTo)
        {
            using var compressedSrc = File.OpenRead(dumpPath);
            using var uncompressedSrc = currentCompression.Decompress(compressedSrc);
            using var compressedDst = File.OpenWrite(compressedDumpPath);
            using var compressorStream = compressTo.Compress(compressedDst);
            uncompressedSrc.CopyTo(compressorStream);
        }
    }

    internal class CommentsIntoChunks : IDisposable
    {
        private readonly string BaseFilePath;
        private readonly string FileExtension;
        private readonly long MaxBytesPerFile;
        private readonly Task CompressionTask;
        private readonly ChannelReader<byte[]> BufferPool;
        private readonly ChannelWriter<WriterCommand> CompressorCmds;
        private readonly ICompressor _compressor;
        private const int BufferCount = 5;
        private const int BufferSize = 1024 * 1024 * 100;
        private int FileCount = 0;
        private long WrittenBytes = 0;
        private CommentBuffer CompressionBuffer;

        public CommentsIntoChunks(string baseFilePath, string fileExtension, long maxBytesPerFile, ICompressor compressor)
        {
            this.BaseFilePath = baseFilePath;
            this.FileExtension = fileExtension;
            this.MaxBytesPerFile = maxBytesPerFile;
            _compressor = compressor;


            Channel<byte[]> bufferPoolChannel = Channel.CreateBounded<byte[]>(BufferCount);
            Channel<WriterCommand> compressorCommandsChannel = Channel.CreateBounded<WriterCommand>(BufferCount);
            this.BufferPool = bufferPoolChannel.Reader;
            this.CompressorCmds = compressorCommandsChannel.Writer;

            var poolWriter = bufferPoolChannel.Writer;
            var cmdReader = compressorCommandsChannel.Reader;
            for (int i = 0; i < BufferCount; i++)
            {
                if (!poolWriter.TryWrite(new byte[BufferSize]))
                {
                    throw new Exception("Failed to put a buffer in the array pool channel when filling it initially with arrays.");
                }
            }

            this.CompressionTask = Task.Run(async () => await CompressBuffers(cmdReader, poolWriter));

            if (!BufferPool.TryRead(out byte[]? buffer))
            {
                throw new Exception("Failed to fetch buffer while one should be available.");
            }
            this.CompressionBuffer = new CommentBuffer(buffer);
        }

        public async ValueTask HandleComment(ReadOnlyMemory<byte> commentJSon, bool foundAllTerms)
        {
            bool chunkIsDone = false;
            if (WrittenBytes + commentJSon.Length > MaxBytesPerFile)
            {
                FileCount++;
                chunkIsDone = true;
                WrittenBytes = 0;
            }

            if (!CompressionBuffer.HasSpaceForComment(commentJSon) || chunkIsDone)
            {
                string? fileName = $"{BaseFilePath}-{FileCount}{FileExtension}";
                await CompressorCmds.WriteAsync(new WriterCommand(fileName, CompressionBuffer, chunkIsDone));
                CompressionBuffer = new CommentBuffer(await BufferPool.ReadAsync());
            }

            CompressionBuffer.AddComment(commentJSon);

            // + 1 because Comments are separated by a new line
            WrittenBytes += commentJSon.Length + 1;
        }

        private async ValueTask CompressBuffers(ChannelReader<WriterCommand> cmdReader, ChannelWriter<byte[]> arrayPool)
        {
            var data = await cmdReader.ReadAsync();
            while (true)
            {
                using var fileStream = File.OpenWrite(data.FileName);
                using var compressionStream = _compressor.Compress(fileStream);

                while (true)
                {
                    compressionStream.Write(data.Buffer.Buffer);
                    await data.Buffer.Return(arrayPool);

                    //Only taken when there is no more work to be done
                    if (!await cmdReader.WaitToReadAsync())
                    {
                        return;
                    }

                    if (!cmdReader.TryRead(out data))
                    {
                        throw new Exception("Expected data to be available but none was in the channel.");
                    }

                    if (data.CreateNewFile)
                    {
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            CompressorCmds.Complete();
            CompressionTask.Wait();
        }
    }

    internal readonly struct WriterCommand
    {
        public readonly string FileName;
        public readonly CommentBuffer Buffer;
        public readonly bool CreateNewFile;

        public WriterCommand(string fileName, CommentBuffer buffer, bool createNewFile)
        {
            this.FileName = fileName;
            this.Buffer = buffer;
            this.CreateNewFile = createNewFile;
        }
    }

    internal struct CommentBuffer
    {
        private readonly byte[] Arr;
        private int BytesInBuffer;

        public ReadOnlySpan<byte> Buffer => Arr.AsSpan(0, BytesInBuffer);

        public CommentBuffer(byte[] array)
        {
            this.Arr = array;
            this.BytesInBuffer = 0;
        }

        public bool HasSpaceForComment(ReadOnlyMemory<byte> comment)
        {
            return BytesInBuffer + comment.Length + 1 < Arr.Length;
        }

        public void AddComment(ReadOnlyMemory<byte> comment)
        {
            comment.CopyTo(new Memory<byte>(Arr, BytesInBuffer, Arr.Length - BytesInBuffer));
            BytesInBuffer += comment.Length;

            Arr[BytesInBuffer] = (byte)'\n';
            BytesInBuffer++;
        }

        public async ValueTask Return(ChannelWriter<byte[]> arrayPool)
        {
            if (Arr != null)
            {
                await arrayPool.WriteAsync(Arr);
            }
        }
    }
}
