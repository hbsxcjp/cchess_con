using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CChess
{
    class CustomData
    {
        public long CreationTime;
        public int Name;
        public int ThreadNum;
    }

    internal class Utility
    {
        public delegate string Show<T>(T t);
        public static string GetString<T>(List<T> items, Show<T> show, string split = "")
        {
            string result = "";
            foreach(T item in items)
                result += show(item) + split;

            return result + String.Format($"【{items.Count}】");
        }

        public static void GetInfo(Dictionary<string, string> info, string infoString)
        {
            var matches = Regex.Matches(infoString, @"\[(\S+) ""(.*)""\]");
            foreach(Match match in matches.Cast<Match>())
                info[match.Groups[1].Value] = match.Groups[2].Value;
        }
        public static string GetString(Dictionary<string, string> info)
            => string.Join("", info.Select(kv => string.Format($"[{kv.Key} \"{kv.Value}\"]\n")));
    }
}
