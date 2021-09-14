using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PushShift_Dump_Parser
{
    internal class CommentIntoDB : IDisposable
    {
        private readonly SQLiteConnection Connection;
        private readonly SQLiteCommand Command;

        public CommentIntoDB(SQLiteConnection connection)
        {
            this.Connection = connection;
            this.Command = Connection.CreateCommand();
        }

        public static async Task ReadCompressedIntoDB(string[] files, )
        {
            foreach (var file in files)
            {
                PushShiftDumpReader reader = new PushShiftDumpReader(file);
                reader.ReadCompressedDumpFile()
            }
        }

        private async ValueTask TransferIntoDB(ReadOnlyMemory<byte> commentJSon, bool foundAllTerms)
        {
            var comment = JsonSerializer.Deserialize<Comment>(commentJSon.Span);

        }

        public void Dispose()
        {
            Connection?.Dispose();
        }
    }
}
