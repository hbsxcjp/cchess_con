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


        public void DownXqbaseManual()
        {
            const int XqbaseInfoCount = 11;
            IEnumerable<string[]> GetInfosListFromXqbase(int start, int end)
            {
                using HttpClient client = new();
                Encoding codec = Encoding.GetEncoding("gb2312");
                Task<string[]>[] taskArray = new Task<string[]>[end - start + 1];
                for(int i = 0;i < taskArray.Length;i++)
                {
                    string uri = string.Format(@"https://www.xqbase.com/xqbase/?gameid={0}", i + start);
                    taskArray[i] = Task<string[]>.Factory.StartNew(() =>
                    {
                        var taskA = client.GetByteArrayAsync(uri);

                        string pattern = @"<title>(.*?)</title>.*?>([^>]+赛[^>]*?)<.*?>(\d+年\d+月(?:\d+日)?)(?: ([^<]*?))?<.*?>黑方 ([^<]*?)<.*?MoveList=(.*?)"".*?>红方 ([^<]*?)<.*?>([A-E]\d{2})\. ([^<]*?)<.*\((.*?)\)</pre>";
                        Regex regex = new(pattern, RegexOptions.Singleline);
                        Match match = regex.Match(codec.GetString(taskA.Result));
                        if(!match.Success)
                            return Array.Empty<string>();

                        // "source", "title", "event", "date", "site", "black", "rowCols", "red", "eccoSn", "eccoName", "win"
                        string[] infos = new string[XqbaseInfoCount];
                        for(int i = 1;i < infos.Length;i++)
                            infos[i] = match.Groups[i].Value;

                        infos[0] = uri;
                        infos[6] = Coord.RowCols(infos[6].Replace("-", "").Replace("+", ""));
                        return infos;
                    });
                }

                Task.WaitAll(taskArray);
                return taskArray.Select(task => task.Result);
            }

            void InsertInfosList(IEnumerable<string[]> infosList)
            {
                using SqliteConnection connection = GetSqliteConnection();
                using var transaction = connection.BeginTransaction();

                var command = connection.CreateCommand();
                List<string> values = new();
                string[] xqbaseInfoKeys = InfoKeys[..XqbaseInfoCount];
                foreach(var key in xqbaseInfoKeys)
                {
                    var paramName = string.Format($"${key}");
                    values.Add(paramName);
                    command.Parameters.Add(new() { ParameterName = paramName });
                }
                var fields = string.Join(", ", xqbaseInfoKeys.Select(key => string.Format($"'{key}'")));
                command.CommandText = $"INSERT INTO {manualTableName} ({fields}) VALUES ({string.Join(", ", values)})";

                foreach(var infos in infosList)
                {
                    int index = 0;
                    foreach(var value in infos)
                        command.Parameters[index++].Value = value;

                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            InsertInfosList(GetInfosListFromXqbase(6, 10)); //总数:12141
        }

        public static readonly string[] InfoKeys = {
            "source", "title", "event", "date", "site", "black", "rowCols", "red", "eccoSn", "eccoName", "win",
            "opening", "writer", "author", "type", "version", "FEN", "moveString" };

        private static void GetInfo(Dictionary<string, string> info, SqliteDataReader reader)
        {
            for(int index = 0;index < reader.FieldCount;++index)
                info[reader.GetName(index)] = reader.GetString(index);
        }

        //private void InsertInfo(Dictionary<string, string> info)
        //{
        //    SqliteCommand command = new(
        //        string.Format($"INSERT INTO {manualTableName} ({string.Join(", ", info.Keys)}) " +
        //        $"VALUES({string.Join(", ", info.Values.Select(value => string.Format($"'{value}'")))})"),
        //        SqliteConnection);
        //    command.ExecuteNonQuery();
        //}

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
