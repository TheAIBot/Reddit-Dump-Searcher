using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PushShift_Dump_Parser
{
    public class Comment
    {
        public string author { get; set;}
        public string subreddit {get; set; }
        public string parent_id {get; set;}
        public string body {get; set;}
        public string name {get; set;}
        public string subreddit_id {get; set;}
        public string link_id {get; set;}
    }
}
