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



        public override string ToString()
        {
            string result = "";
            var infoList = GetInfoListFromXqbase(6, 10);
            foreach(var info in infoList)
                result += Utility.GetString(info.ToList(), str => str, "\n") + "\n";

            InsertInfoList(infoList);
            return result;
        }

        private void InsertInfoList(IEnumerable<string[]> infoList)
        {
            using var transaction = SqliteConnection.BeginTransaction();
            // 前提条件：infoList中所有info的Keys()全部相同
            string[] infoKeys = manualFieldNames[..xqbaseHtmlFieldCount];
            var command = SqliteConnection.CreateCommand();
            List<string> values = new();
            foreach(var key in infoKeys)
            {
                var paramName = string.Format($"${key}");
                values.Add(paramName);
                command.Parameters.Add(new() { ParameterName = paramName });
            }
            var fields = string.Join(", ", infoKeys.Select(key => string.Format($"'{key}'")));
            command.CommandText = $"INSERT INTO {manualTableName} ({fields}) VALUES ({string.Join(", ", values)})";

            foreach(var info in infoList)
            {
                int index = 0;
                foreach(var value in info)
                    command.Parameters[index++].Value = value;

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private void InsertInfo(Dictionary<string, string> info)
        {
            SqliteCommand command = new(
                string.Format($"INSERT INTO {manualTableName} ({string.Join(", ", info.Keys)}) " +
                $"VALUES({string.Join(", ", info.Values.Select(value => string.Format($"'{value}'")))})"),
                SqliteConnection);
            command.ExecuteNonQuery();
        }

        private static void GetInfo(Dictionary<string, string> info, SqliteDataReader reader)
        {
            for(int index = 0;index < reader.FieldCount;++index)
                info[reader.GetName(index)] = reader.GetString(index);
        }

        private IEnumerable<string[]> GetInfoListFromXqbase(int start, int end)
        {
            void GetInfoXqbase(string[] info, string htmlString)
            {
                //{ "TITLE", "EVENT", "DATE", "SITE", "BLACK", "MOVESTR", "RED", "ECCOSN", "ECCONAME", "RESULT" };
                string pattern = @"<title>(.*?)</title>.*?>([^>]+赛[^>]*?)<.*?>(\d+年\d+月(?:\d+日)?)(?: ([^<]*?))?<.*?>黑方 ([^<]*?)<.*?MoveList=(.*?)"".*?>红方 ([^<]*?)<.*?>([A-E]\d{2})\. ([^<]*?)<.*\((.*?)\)</pre>";
                Regex regex = new(pattern, RegexOptions.Singleline);
                Match match = regex.Match(htmlString);
                if(match.Success)
                    for(int i = 1;i < info.Length;i++)
                        info[i] = i != 6 ? match.Groups[i].Value : match.Groups[i].Value.Replace("-", "");
            }

            Encoding codec = Encoding.GetEncoding("gb2312");
            Task<string[]>[] taskArray = new Task<string[]>[end - start + 1]; //总数:12141
            for(int i = 0;i < taskArray.Length;i++)
            {
                string uri = string.Format(@"https://www.xqbase.com/xqbase/?gameid={0}", i + start);
                taskArray[i] = Task<string[]>.Factory.StartNew(() =>
                {
                    string[] info = new string[xqbaseHtmlFieldCount];
                    info[0] = uri;
                    var taskA = Client.GetByteArrayAsync(uri);
                    GetInfoXqbase(info, codec.GetString(taskA.Result));
                    return info;
                });
            }

            Task.WaitAll(taskArray);
            return taskArray.Select(task => task.Result);
        }

        private void InitDatabase()
        {
            SqliteCommand command = new(string.Format(
                $"CREATE TABLE {manualTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                $"{string.Join(",", manualFieldNames.Select(field => field + " TEXT"))})"),
                SqliteConnection);
            command.ExecuteNonQuery();
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
                    InitDatabase();

                return _connection;
            }
        }
        private HttpClient Client { get { return _client ??= new(); } }

        private readonly string databaseFileName = "data.db";
        private readonly string manualTableName = "manual";
        private readonly string[] manualFieldNames = {
            "SOURCE", "TITLE", "EVENT", "DATE", "SITE", "BLACK", "MOVESTR", "RED", "ECCOSN", "ECCONAME", "RESULT",
            "OPENING", "WRITER", "AUTHOR", "TYPE", "VERSION", "FEN", "ROWCOLS", "CALUATE_ECCOSN" };
        private readonly int xqbaseHtmlFieldCount = 11;

        private SqliteConnection? _connection;
        private HttpClient? _client = null;
    }
}
