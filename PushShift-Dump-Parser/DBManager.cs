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
            @"CREATE TABLE users(
                id INTEGER PRIMARY KEY AUTOINCREMENT, 
                name TEXT
            )");
            CreateTable(cmd, 
            @"CREATE TABLE submissions(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                sub_id TEXT,
                title TEXT,
                userID INTEGER,
                FOREIGN KEY(userID) REFERENCES users(id)
            )");
            CreateTable(cmd, 
            @"CREATE TABLE comments(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                com_id TEXT,
                content TEXT,
                userID INTEGER,
                submissionID INTEGER,
                parentCommentID INTEGER,
                FOREIGN KEY(userID) REFERENCES users(id)
                FOREIGN KEY(submissionID) REFERENCES submissions(id)
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
