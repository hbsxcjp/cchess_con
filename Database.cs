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
    internal enum ManualField
    {
        Source, Title, Event, Date, Site, Black, RowCols, Red, EccoSn, EccoName, Win,
        Opening, Writer, Author, Type, Version, FEN, MoveString
    }
    internal class Database
    {
        public List<Manual> GetManuals(string condition = "1")
        {
            static Dictionary<string, string> GetInfo(SqliteDataReader reader)
            {
                Dictionary<string, string> info = new();
                for(int index = 0;index < reader.FieldCount;++index)
                    if(!reader.IsDBNull(index))
                        info[reader.GetName(index)] = reader.GetString(index);

                return info;
            }

            List<Manual> manuals = new();
            using SqliteConnection connection = GetSqliteConnection();
            SqliteCommand command = new(ManualSelectCommandText(condition), connection);
            using SqliteDataReader reader = command.ExecuteReader();
            while(reader.Read())
                manuals.Add(new(GetInfo(reader)));

            return manuals;
        }

        public void StorageFileManual(IEnumerable<string> fileNames)
        {
            IEnumerable<Manual> manuals = fileNames.Select(fileName =>
            {
                Manual manual = new(fileName);
                manual.SetDatabaseField(fileName);
                return manual;
            });

            InsertInfoList(manuals.Select(manual => manual.Info));
        }

        //总界限:1~12141
        public void DownXqbaseManual(int start = 1, int end = 5)
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
                Dictionary<string, string> info = new() { { GetInfoKey(ManualField.Source), uri } };
                for(int i = 1;i < XqbaseInfoCount;i++)
                    info[GetInfoKey((ManualField)i)] = i != 6 ? match.Groups[i].Value
                        : Coord.RowCols(match.Groups[i].Value.Replace("-", "").Replace("+", ""));

                return info;
            }

            Task<Dictionary<string, string>>[] taskArray = new Task<Dictionary<string, string>>[end - start + 1];
            for(int i = 0;i < taskArray.Length;i++)
            {
                string uri = string.Format(@"https://www.xqbase.com/xqbase/?gameid={0}", i + start);
                taskArray[i] = Task<Dictionary<string, string>>.Factory.StartNew(() => GetInfo(uri));
            }
            Task.WaitAll(taskArray);
            InsertInfoList(taskArray.Select(task => task.Result));
        }

        public static string GetInfoKey(ManualField field) => _infoKeys[(int)field];

        private void InsertInfoList(IEnumerable<Dictionary<string, string>> infoList, bool unequal = true)
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
            command.CommandText = $"INSERT INTO {_manualTableName} ({fields}) " +
                $"VALUES ({JoinEnumableString(infoKeys.Select(key => ParamName(key)))})";

            foreach(var info in infoList)
            {
                if(unequal && ExistsManual(connection,
                    FieldEqualCondition(ManualField.Source, info[GetInfoKey(ManualField.Source)])))
                    continue;

                foreach(var key in infoKeys)
                    command.Parameters[ParamName(key)].Value = info[key];

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        private bool ExistsManual(SqliteConnection connection, string condition)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = ManualSelectCommandText(condition);
            using var reader = command.ExecuteReader();
            return reader.Read();
        }
        private string ManualSelectCommandText(string condition) => $"SELECT * FROM {_manualTableName} WHERE {condition}";
        private static string FieldEqualCondition(ManualField field, string value) => $" {GetInfoKey(field)} == '{value}'";
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
                        string.Format($"CREATE TABLE {_manualTableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                        $"{string.Join(",", _infoKeys.Select(field => field + " TEXT"))})"), };
                SqliteCommand command = connection.CreateCommand();
                foreach(var str in commandString)
                {
                    command.CommandText = str;
                    command.ExecuteNonQuery();
                }
            }

            return connection;
        }

        private static readonly string[] _infoKeys = {
            "source", "title", "event", "date", "site", "black", "rowCols", "red", "eccoSn", "eccoName", "win",
            "opening", "writer", "author", "type", "version", "FEN", "moveString" };
        private readonly string _manualTableName = "manual";
    }
}
