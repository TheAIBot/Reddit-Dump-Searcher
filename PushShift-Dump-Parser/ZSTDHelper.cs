using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ZstdNet;

namespace PushShift_Dump_Parser
{
    internal static class ZSTDHelper
    {
        public static void CompressFolder(string folderPath)
        {
            Parallel.ForEach(Directory.GetFiles(folderPath), fileName =>
            {
                if (fileName.Contains(".zstd"))
                {
                    return;
                }

                if (File.Exists(fileName + ".zstd"))
                {
                    return;
                }

                Console.WriteLine($"Starting to compress: {fileName}");
                CompressDump(fileName, fileName + ".zstd");
                Console.WriteLine($"Finished to compressing: {fileName}");
            });
        }

        public static void ChangeCompressionLevel(string folderPath, int newCompressionLevel)
        {
            Parallel.ForEach(Directory.GetFiles(folderPath), fileName =>
            {
                string dstFileName = fileName + "-temp";

                using (var compressionOptions = new CompressionOptions(newCompressionLevel))
                using (var srcFile = File.OpenRead(fileName))
                using (var decompressor = new DecompressionStream(srcFile))
                using (var dstFile = File.OpenWrite(dstFileName))
                using (var compressor = new CompressionStream(dstFile, compressionOptions))
                {
                    decompressor.CopyTo(compressor);
                }

                File.Delete(fileName);
                File.Move(dstFileName, fileName);
            });
        }

        public static async Task SplitFilesIntoSmallerCompressedFiles(string srcDir, string dstDir)
        {
            Directory.CreateDirectory(dstDir);
            ActionBlock<string> actionExecutor = new ActionBlock<string>(srcFileName =>
            {
                string baseDstFileName = Path.Combine(dstDir, Path.GetFileNameWithoutExtension(srcFileName));
                string fileExtension = Path.GetExtension(srcFileName);
                long maxFileSize = 1024 * 1024 * 1024 * 6L;

                var commentSearcher = new PushShiftDumpReader(srcFileName);
                using var commentSplitter = new CommentsIntoChunks(baseDstFileName, fileExtension, maxFileSize);

                commentSearcher.ReadCompressedDumpFile(Array.Empty<string>(), commentSplitter.HandleComment);
            },
            new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                MaxMessagesPerTask = 1
            });

            foreach (var file in Directory.GetFiles(srcDir))
            {
                actionExecutor.Post(file);
            }
            actionExecutor.Complete();
            await actionExecutor.Completion;
        }

        public static void CompressDump(string dumpPath, string compressedDumpPath)
        {
            using var compressedSrc = File.OpenRead(dumpPath);
            using var compressedDst = File.OpenWrite(compressedDumpPath);
            using var compressor = new CompressionStream(compressedDst);
            compressedSrc.CopyTo(compressor);
        }
    }

    internal class CommentsIntoChunks : IDisposable
    {
        private readonly string BaseFilePath;
        private readonly string FileExtension;
        private readonly long MaxBytesPerFile;
        private readonly Task CompressionTask;
        private readonly BlockingCollection<byte[]> PrivatePool;
        private readonly BlockingCollection<(CommentBuffer buffer, string fileName)> CompressionCom;
        private int FileCount = 0;
        private long WrittenBytes = 0;
        private CommentBuffer CompressionBuffer;

        public CommentsIntoChunks(string baseFilePath, string fileExtension, long maxBytesPerFile)
        {
            this.BaseFilePath = baseFilePath;
            this.FileExtension = fileExtension;
            this.MaxBytesPerFile = maxBytesPerFile;
            this.PrivatePool = new BlockingCollection<byte[]>();
            for (int i = 0; i < 5; i++)
            {
                PrivatePool.Add(new byte[1024 * 1024 * 100]);
            }

            this.CompressionCom = new BlockingCollection<(CommentBuffer buffer, string fileName)>(new ConcurrentQueue<(CommentBuffer buffer, string fileName)>());
            this.CompressionTask = Task.Run(() => CompressBuffers(CompressionCom, PrivatePool));
            this.CompressionBuffer = new CommentBuffer(PrivatePool);
        }

        public void HandleComment(Memory<byte> commentJSon, bool foundAllTerms)
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
                string fileName = $"{BaseFilePath}-{FileCount}{FileExtension}";
                CompressionCom.Add((CompressionBuffer, fileName));
                CompressionBuffer = new CommentBuffer(PrivatePool);
            }

            CompressionBuffer.AddComment(commentJSon);

            // + 1 because Comments are separated by a new line
            WrittenBytes += commentJSon.Length + 1;
        }

        private static void CompressBuffers(BlockingCollection<(CommentBuffer buffer, string fileName)> bufferFetcher, BlockingCollection<byte[]> arrayPool)
        {
            var data = bufferFetcher.Take();
            while (true)
            {
                using var fileStream = File.OpenWrite(data.fileName);
                using var compressionStream = new CompressionStream(fileStream);

                while (true)
                {
                    compressionStream.Write(data.buffer.Buffer);
                    data.buffer.Return(arrayPool);

                    if (!bufferFetcher.TryTake(out data, -1))
                    {
                        return;
                    }
                    else if (fileStream.Name != data.fileName)
                    {
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            CompressionCom.CompleteAdding();
            CompressionTask.Wait();
            CompressionCom.Dispose();
        }
    }

    internal struct CommentBuffer
    {
        private readonly byte[] Arr;
        private int BytesInBuffer;

        public ReadOnlySpan<byte> Buffer => Arr.AsSpan(0, BytesInBuffer);

        public CommentBuffer(BlockingCollection<byte[]> arrayPool)
        {
            this.Arr = arrayPool.Take();
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

        public void Return(BlockingCollection<byte[]> arrayPool)
        {
            if (Arr != null)
            {
                arrayPool.Add(Arr);
            }
        }
    }
}
