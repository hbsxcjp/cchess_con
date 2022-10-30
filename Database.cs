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
            var infoList = GetInfoListFromXqbase(Client);
            foreach(var info in infoList)
            {
                result += Utility.GetString(info) + "\n";
                //InsertInfo(info);
            }
            InsertInfoList(infoList);

            return result;
        }

        void InsertInfoList(IEnumerable<Dictionary<string, string>> infoList)
        {
            using var transaction = SqliteConnection.BeginTransaction();
            // 前提条件：infoList中所有info的Keys()全部相同
            var infoKeys = infoList.First().Keys;
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
                foreach(var key in infoKeys)
                    command.Parameters[index++].Value = info[key];

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        void InsertInfo(Dictionary<string, string> info)
        {
            SqliteCommand command = new(
                string.Format($"INSERT INTO {manualTableName} ({string.Join(", ", info.Keys)}) " +
                $"VALUES({string.Join(", ", info.Values.Select(value => string.Format($"'{value}'")))})"),
                SqliteConnection);
            command.ExecuteNonQuery();
        }

        void GetInfo(Dictionary<string, string> info, SqliteDataReader reader)
        {
            for(int index = 0;index < reader.FieldCount;++index)
                info[reader.GetName(index)] = reader.GetString(index);
        }

        public IEnumerable<Dictionary<string, string>> GetInfoListFromXqbase(HttpClient client)
        {
            void GetInfoXqbase(Dictionary<string, string> info, string htmlString)
            {
                //string[] keys = { "TITLE", "EVENT", "DATE", "SITE", "BLACK", "MOVESTR", "RED", "ECCOSN", "ECCONAME", "RESULT" };
                string[] keys = manualFieldNames[..10];
                string pattern = @"<title>(.*?)</title>.*?>([^>]+赛[^>]*?)<.*?>(\d+年\d+月(?:\d+日)?)(?: ([^<]*?))?<.*?>黑方 ([^<]*?)<.*?MoveList=(.*?)"".*?>红方 ([^<]*?)<.*?>([A-E]\d{2})\. ([^<]*?)<.*\((.*?)\)</pre>";
                Regex regex = new(pattern, RegexOptions.Singleline);
                Match match = regex.Match(htmlString);
                if(match.Success)
                    for(int i = 0;i < keys.Length;i++)
                        info[keys[i]] = i != 5 ? match.Groups[i + 1].Value : match.Groups[i + 1].Value.Replace("-", "");
            }

            Encoding codec = Encoding.GetEncoding("gb2312");
            Task<Dictionary<string, string>>[] taskArray = new Task<Dictionary<string, string>>[5]; //总数:12141
            for(int i = 0;i < taskArray.Length;i++)
            {
                string uri = string.Format(@"https://www.xqbase.com/xqbase/?gameid={0}", i + 1);
                taskArray[i] = Task<Dictionary<string, string>>.Factory.StartNew(() =>
                {
                    Dictionary<string, string> info = new() { { "SOURCE", uri } };
                    var taskA = client.GetByteArrayAsync(uri);
                    GetInfoXqbase(info, codec.GetString(taskA.Result));
                    return info;
                });
            }

            Task.WaitAll(taskArray);
            return taskArray.Select(task => task.Result);
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

        private void InitDatabase()
        {
            SqliteCommand command = new(string.Format(
                $"CREATE TABLE {manualTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                $"{string.Join(",", manualFieldNames.Select(field => field + " TEXT"))})"),
                SqliteConnection);
            command.ExecuteNonQuery();
        }

        private readonly string databaseFileName = "data.db";
        private readonly string manualTableName = "manual";
        private readonly string[] manualFieldNames = {
            "TITLE", "EVENT", "DATE", "SITE", "BLACK", "MOVESTR", "RED", "ECCOSN", "ECCONAME", "RESULT",
            "OPENING", "WRITER", "AUTHOR", "TYPE", "VERSION", "FEN", "ROWCOLS", "SOURCE", "CALUATE_ECCOSN" };

        private SqliteConnection? _connection;
        private HttpClient? _client = null;
    }
}
