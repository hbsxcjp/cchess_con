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
        public static readonly string[] InfoBaseKeys = {
            "source", "title", "event", "date", "site", "black", "rowCols", "red", "eccoSn", "eccoName", "win"};
        public static readonly string[] InfoExtendKeys = { "moveString", "opening", "writer", "author", "type", "version", "FEN" };


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

        public void DownXqbaseManual()
        {
            IEnumerable<string[]> GetInfosListFromXqbase(int start, int end)
            {
                Encoding codec = Encoding.GetEncoding("gb2312");
                Task<string[]>[] taskArray = new Task<string[]>[end - start + 1];
                for(int i = 0;i < taskArray.Length;i++)
                {
                    string uri = string.Format(@"https://www.xqbase.com/xqbase/?gameid={0}", i + start);
                    taskArray[i] = Task<string[]>.Factory.StartNew(() =>
                    {
                        var taskA = Client.GetByteArrayAsync(uri);

                        //{ "TITLE", "EVENT", "DATE", "SITE", "BLACK", "MOVESTR", "RED", "ECCOSN", "ECCONAME", "RESULT" };
                        string pattern = @"<title>(.*?)</title>.*?>([^>]+赛[^>]*?)<.*?>(\d+年\d+月(?:\d+日)?)(?: ([^<]*?))?<.*?>黑方 ([^<]*?)<.*?MoveList=(.*?)"".*?>红方 ([^<]*?)<.*?>([A-E]\d{2})\. ([^<]*?)<.*\((.*?)\)</pre>";
                        Regex regex = new(pattern, RegexOptions.Singleline);
                        Match match = regex.Match(codec.GetString(taskA.Result));
                        if(!match.Success)
                            return Array.Empty<string>();

                        string[] infos = new string[InfoBaseKeys.Length];
                        infos[0] = uri;
                        for(int i = 1;i < infos.Length;i++)
                            infos[i] = i != 6 ? match.Groups[i].Value : Coord.RowCols(match.Groups[i].Value.Replace("-", "").Replace("+", ""));
                        return infos;
                    });
                }

                Task.WaitAll(taskArray);
                return taskArray.Select(task => task.Result);
            }

            void InsertInfosList(IEnumerable<string[]> infosList)
            {
                using var transaction = SqliteConnection.BeginTransaction();

                var command = SqliteConnection.CreateCommand();
                List<string> values = new();
                foreach(var key in InfoBaseKeys)
                {
                    var paramName = string.Format($"${key}");
                    values.Add(paramName);
                    command.Parameters.Add(new() { ParameterName = paramName });
                }
                var fields = string.Join(", ", InfoBaseKeys.Select(key => string.Format($"'{key}'")));
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

        private SqliteConnection SqliteConnection
        {
            get
            {
                if(_connection != null)
                    return _connection;

                bool fileExists = File.Exists(databaseFileName);
                if(!fileExists)
                    using(File.Create(databaseFileName)) { };

                _connection = new("Data Source=" + databaseFileName);
                _connection.Open();
                if(!fileExists)
                {
                    string[] commandString = new string[]{
                        string.Format($"CREATE TABLE {manualTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        $"{string.Join(",", (InfoBaseKeys.Concat(InfoExtendKeys)).Select(field => field + " TEXT"))})"), };
                    SqliteCommand command = _connection.CreateCommand();
                    foreach(var str in commandString)
                    {
                        command.CommandText = str;
                        command.ExecuteNonQuery();
                    }
                }

                return _connection;
            }
        }
        private HttpClient Client { get { return _client ??= new(); } }

        private readonly string databaseFileName = "data.db";
        private readonly string manualTableName = "manual";

        private SqliteConnection? _connection;
        private HttpClient? _client = null;
    }
}
