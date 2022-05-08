using PushShift_Dump_Parser;
using System.IO;



//await ParallelDumpReader.SearchFiles(Directory.GetFiles(dumpsDir), searchTerms, new ZstdCompressor());
//await ParallelDumpReader.SearchFiles(Directory.GetFiles(chunksDir), searchTerms, new ZstdCompressor());

//await ZSTDHelper.SplitFilesIntoSmallerCompressedFiles(dumpsDir, chunksDir);

CompressorHelper.CompressFolder(dumpsDir, new ZstdCompressor(), new Lz4Compressor());

//using var db = new RedditContext();


