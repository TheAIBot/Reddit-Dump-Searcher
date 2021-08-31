using System.Threading.Tasks.Dataflow;

namespace PushShift_Dump_Parser
{
    internal static class ParallelDumpReader
    {
        public static async Task SearchFiles(string[] files, string[] searchTerms)
        {
            PushShiftDumpReader[] readers = files.Select(x => new PushShiftDumpReader(x)).ToArray();
            ActionBlock<PushShiftDumpReader> actionExecutor = new ActionBlock<PushShiftDumpReader>(x => x.ReadCompressedDumpFile(searchTerms),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount - 1,
                    MaxMessagesPerTask = 1
                });

            foreach (var reader in readers)
            {
                actionExecutor.Post(reader);
            }

            DateTime startingTime = DateTime.Now;

            int oldSearchedTotal = 0;
            Queue<int> linesPerSecHistory = new Queue<int>();
            PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync())
            {
                int searchedTotal = readers.Sum(x => x.LinesSearched);
                int linesWithTerms = readers.Sum(x => x.LinesWithTerms);

                int linesPerSec = searchedTotal - oldSearchedTotal;
                int TimeToAverageOver = 4;
                int averageLinesPerSec = AddAndGetAverage(linesPerSecHistory, linesPerSec, TimeToAverageOver);
                oldSearchedTotal = searchedTotal;

                Console.WriteLine($"Searched: {searchedTotal:N0}, Comments/sec: {averageLinesPerSec:N0}, Comments with terms: {linesWithTerms:N0}");

                if (readers.All(x => x.IsDoneSearching))
                {
                    break;
                }
            }

            DateTime endTime = DateTime.Now;
            Console.WriteLine($"Total Time: {(endTime - startingTime).TotalSeconds}s");
        }

        private static int AddAndGetAverage(Queue<int> queue, int newValue, int maxQueueSize)
        {
            queue.Enqueue(newValue);

            if (queue.Count > maxQueueSize)
            {
                queue.Dequeue();
            }

            return (int)queue.Average();
        }
    }
}
