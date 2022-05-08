using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushShift_Dump_Parser
{
    internal static class SimpleDumpReader
    {
        public static void ReadDumpFile(string dumpPath, string[] searchTerms)
        {
            var fileLines = File.ReadLines(dumpPath);

            Stopwatch watch = new Stopwatch();
            watch.Start();

            int linesSearched = 0;
            int oldLinesSearched = 0;
            int linesWithTerms = 0;

            foreach (var line in fileLines)
            {
                bool foundAllTerms = true;
                foreach (var term in searchTerms)
                {
                    if (!line.Contains(term))
                    {
                        foundAllTerms = false;
                        break;
                    }
                }

                if (foundAllTerms)
                {
                    linesWithTerms++;
                }
                linesSearched++;

                if (watch.ElapsedMilliseconds >= 1000)
                {
                    int linesPerSec = linesSearched - oldLinesSearched;
                    oldLinesSearched = linesSearched;
                    watch.Restart();
                    Console.WriteLine($"Searched: {linesSearched:N0}, Comments/sec: {linesPerSec:N0}, Comments with terms: {linesWithTerms:N0}");
                }
            }

            Console.WriteLine($"Searched: {linesSearched:N0} Comments with terms: {linesWithTerms:N0}");
        }
    }
}
