namespace PushShift_Dump_Parser
{
    internal class DumpReader
    {
        public int LinesSearched { get; protected set; } = 0;
        public int LinesWithTerms { get; protected set; } = 0;
        public bool IsDoneSearching { get; protected set; } = false;

        protected void ResetStats()
        {
            LinesSearched = 0;
            LinesWithTerms = 0;
            IsDoneSearching = false;
        }
    }
}
