using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PushShift_Dump_Parser
{
    internal static class DBManager
    {
        internal static void DeleteTables(SQLiteConnection connection)
        {
            using var cmd = new SQLiteCommand(connection);

            ExecuteCommand(cmd, "DROP TABLE IF EXISTS users");
            ExecuteCommand(cmd, "DROP TABLE IF EXISTS submissions");
            ExecuteCommand(cmd, "DROP TABLE IF EXISTS comments");
            Console.WriteLine("Dropped all tables.");
        }

        internal static void CreateTables(SQLiteConnection connection)
        {
            using var cmd = new SQLiteCommand(connection);
            CreateTable(cmd, 
            @"CREATE TABLE User(
                UserID INTEGER PRIMARY KEY AUTOINCREMENT, 
                Name TEXT
            )");
            CreateTable(cmd,
            @"CREATE TABLE Submission(
                SubmissionID INTEGER PRIMARY KEY AUTOINCREMENT,
                RedditSubmissionID TEXT,
                Title TEXT,
                UserID INTEGER,
                FOREIGN KEY(UserID) REFERENCES User(UserID)
            )");
            CreateTable(cmd,
            @"CREATE TABLE Comment(
                CommentID INTEGER PRIMARY KEY AUTOINCREMENT,
                RedditCommentID TEXT,
                Content TEXT,
                UserID INTEGER,
                SubmissionID INTEGER,
                ParentRedditCommentID INTEGER,
                FOREIGN KEY(UserID) REFERENCES User(UserID)
                FOREIGN KEY(SubmissionID) REFERENCES Submission(SubmissionID)
            )");
            Console.WriteLine("Created all tables.");
        }

        internal static void CreateTable(SQLiteCommand cmdExe, string table)
        {
            ExecuteCommand(cmdExe, table);
        }

        internal static void ExecuteCommand(SQLiteCommand cmdExe, string command)
        {
            cmdExe.CommandText = command;
            cmdExe.ExecuteNonQuery();
        }

        internal static void AddUser(SQLiteConnection connection, string username)
        {
            using var cmd = new SQLiteCommand(connection);
            AddUser(cmd, username);
        }

        internal static void AddUser(SQLiteCommand cmdExe, string username)
        {
            cmdExe.CommandText = "INSERT INTO users(name) VALUES(@name)";

            cmdExe.Parameters.AddWithValue("@name", username);
            cmdExe.Prepare();

            cmdExe.ExecuteNonQuery();
        }
    }
}
