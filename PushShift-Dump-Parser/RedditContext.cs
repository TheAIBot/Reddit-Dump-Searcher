using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PushShift_Dump_Parser
{
    internal class User
    {
        public int UserID { get; set; }
        public string Name { get; set; }
    }
    internal class Submission
    {
        public int SubmissionID { get; set; }
        public string RedditSubmissionID { get; set; }
        public string Subreddit { get; set; }
        public string Title { get; set; }
        public int UserID { get; set; }
        public User User { get; set; }
    }
    internal class Comment
    {
        public int CommentID { get; set; }
        public string RedditCommentID { get; set; }
        public string ParentRedditCommentID { get; set; }
        public string Content { get; set; }
        public int UserID { get; set; }
        public User User { get; set; }
        public int SubmissionID { get; set; }
        public Submission Submission { get; set; }
    }

    internal class RedditContext : DbContext
    {
        public DbSet<User> Users {  get; set; }
        public DbSet<Submission> Submissions {  get; set; }
        public DbSet<Comment> Comments {  get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=./reddit.db");
        }
    }
}
