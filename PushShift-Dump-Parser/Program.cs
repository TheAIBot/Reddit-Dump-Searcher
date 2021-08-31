using PushShift_Dump_Parser;
using System.Diagnostics;
using System.IO;
string dumpsDir = @"...";
string chunksDir = @"...";

string[] searchTerms = new string[] { "..." };

//await ParallelDumpReader.SearchFiles(Directory.GetFiles(dumpsDir), searchTerms);
//await ParallelDumpReader.SearchFiles(Directory.GetFiles(chunksDir), searchTerms);

await ZSTDHelper.SplitFilesIntoSmallerCompressedFiles(dumpsDir, chunksDir);


