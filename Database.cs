using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace CChess
{
    internal class Database
    {

        public static readonly string[] InfoKeys = {
            "source", "title", "event", "date", "site", "black", "rowCols", "red", "eccoSn", "eccoName", "win",
            "opening", "writer", "author", "type", "version", "FEN", "moveString" };


        public void DownXqbaseManual()
        {
            const int XqbaseInfoCount = 11;
            using HttpClient client = new();
            Encoding codec = Encoding.GetEncoding("gb2312");
            Dictionary<string, string> GetInfo(string uri)
            {
                string pattern = @"<title>(.*?)</title>.*?>([^>]+赛[^>]*?)<.*?>(\d+年\d+月(?:\d+日)?)(?: ([^<]*?))?<.*?>黑方 ([^<]*?)<.*?MoveList=(.*?)"".*?>红方 ([^<]*?)<.*?>([A-E]\d{2})\. ([^<]*?)<.*\((.*?)\)</pre>";
                var taskA = client.GetByteArrayAsync(uri);
                Match match = Regex.Match(codec.GetString(taskA.Result), pattern, RegexOptions.Singleline);
                if(!match.Success)
                    return new();

                // "source", "title", "event", "date", "site", "black", "rowCols", "red", "eccoSn", "eccoName", "win"
                Dictionary<string, string> info = new() { { InfoKeys[0], uri } };
                for(int i = 1;i < XqbaseInfoCount;i++)
                    info[InfoKeys[i]] = i != 6 ? match.Groups[i].Value
                        : Coord.RowCols(match.Groups[i].Value.Replace("-", "").Replace("+", ""));

                return info;
            }

            int start = 1, end = 5; //总数:12141
            Task<Dictionary<string, string>>[] taskArray = new Task<Dictionary<string, string>>[end - start + 1];
            for(int i = 0;i < taskArray.Length;i++)
            {
                string uri = string.Format(@"https://www.xqbase.com/xqbase/?gameid={0}", i + start);
                taskArray[i] = Task<Dictionary<string, string>>.Factory.StartNew(() => GetInfo(uri));
            }
            Task.WaitAll(taskArray);
            InsertInfoList(taskArray.Select(task => task.Result));
        }

        private void InsertInfoList(IEnumerable<Dictionary<string, string>> infoList)
        {
            using SqliteConnection connection = GetSqliteConnection();
            using var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            // 要求：所有Info的Keys都相同
            var infoKeys = infoList.First().Keys;
            static string ParamName(string key) => "$" + key;
            foreach(var key in infoKeys)
                command.Parameters.Add(new() { ParameterName = ParamName(key) });

            static string JoinEnumableString(IEnumerable<string> strings) => string.Join(", ", strings);
            var fields = JoinEnumableString(infoKeys.Select(key => string.Format($"'{key}'")));
            command.CommandText = $"INSERT INTO {manualTableName} ({fields}) " +
                $"VALUES ({JoinEnumableString(infoKeys.Select(key => ParamName(key)))})";

            foreach(var info in infoList)
            {
                foreach(var key in infoKeys)
                    command.Parameters[ParamName(key)].Value = info[key];

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void GetInfo(Dictionary<string, string> info, SqliteDataReader reader)
        {
            for(int index = 0;index < reader.FieldCount;++index)
                info[reader.GetName(index)] = reader.GetString(index);
        }

        private SqliteConnection GetSqliteConnection()
        {
            string databaseFileName = "data.db";
            bool fileExists = File.Exists(databaseFileName);
            if(!fileExists)
                using(File.Create(databaseFileName)) { };

            SqliteConnection connection = new("Data Source=" + databaseFileName);
            connection.Open();
            if(!fileExists)
            {
                string[] commandString = new string[]{
                        string.Format($"CREATE TABLE {manualTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        $"{string.Join(",", InfoKeys.Select(field => field + " TEXT"))})"), };
                SqliteCommand command = connection.CreateCommand();
                foreach(var str in commandString)
                {
                    command.CommandText = str;
                    command.ExecuteNonQuery();
                }
            }

            return connection;
        }

        private readonly string manualTableName = "manual";
    }
}
