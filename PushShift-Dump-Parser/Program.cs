using PushShift_Dump_Parser;
using System.IO;

string dumpsDir = @"Q:\Github\Reddit-Dump-Searcher\PushShift-Dump-Parser\pushshift-dump";
string chunksDir = @"Q:\Github\Reddit-Dump-Searcher\PushShift-Dump-Parser\pushshift-dumps-chunks";

string[] searchTerms = new string[] { "hand", "dryer" };
//string[] searchTerms = new string[] { "lol" };

//await ParallelDumpReader.SearchFiles(Directory.GetFiles(dumpsDir), searchTerms, new ZstdCompressor());
//await ParallelDumpReader.SearchFiles(Directory.GetFiles(chunksDir), searchTerms, new ZstdCompressor());

//await ZSTDHelper.SplitFilesIntoSmallerCompressedFiles(dumpsDir, chunksDir);

CompressorHelper.CompressFolder(dumpsDir, new ZstdCompressor(), new Lz4Compressor());

//using var db = new RedditContext();


