using System;
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

        private class CommentsIntoChunks : IDisposable
        {
            private readonly string BaseFilePath;
            private readonly string FileExtension;
            private readonly long MaxBytesPerFile;
            private int FileCount = 0;
            private long WrittenBytes = 0;
            private Stream? FileStream = null;
            private Stream? CompressionStream = null;

            public CommentsIntoChunks(string baseFilePath, string fileExtension, long maxBytesPerFile)
            {
                this.BaseFilePath = baseFilePath;
                this.FileExtension = fileExtension;
                this.MaxBytesPerFile = maxBytesPerFile;
            }

            public void HandleComment(Memory<byte> commentJSon, bool foundAllTerms)
            {
                if (WrittenBytes + commentJSon.Length > MaxBytesPerFile)
                {
                    CloseStreams();
                    WrittenBytes = 0;
                }

                if (FileStream == null)
                {
                    string fileName = BaseFilePath + $"-{FileCount++}" + FileExtension;
                    FileStream = File.OpenWrite(fileName);
                    CompressionStream = new CompressionStream(FileStream);
                }

                if (WrittenBytes > 0)
                {
                    CompressionStream.WriteByte((byte)'\n');
                    WrittenBytes++;
                }
                CompressionStream.Write(commentJSon.Span);
                WrittenBytes += commentJSon.Length;
            }

            private void CloseStreams()
            {
                CompressionStream?.Dispose();
                FileStream?.Dispose();

                FileStream = null;
                CompressionStream = null;
            }

            public void Dispose()
            {
                CloseStreams();
            }
        }

        public static async Task SplitFilesIntoSmallerCompressedFiles(string srcDir, string dstDir)
        {
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
}
