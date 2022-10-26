using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CChess
{
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


    }
}
