using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CChess
{
    internal class Utility
    {

        public static string GetString<T>(List<T> items, string split = "")
        {
            string result = "";
            foreach(T item in items)
                result += item?.ToString() + split;

            return result + String.Format($"【{items.Count}】");
        }
    }
}
