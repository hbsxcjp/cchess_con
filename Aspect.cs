using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal struct Aspect
    {
        public Aspect(string fen, PieceColor color, int data)
        {
            Fen = fen;
            Color = color;
            Data = data;
        }

        new public string ToString() => string.Format($"{Fen}_{Color}_{Data}");

        public readonly string Fen;
        public readonly PieceColor Color;
        public readonly int Data;
    }

    internal class Aspects
    {
        public Aspects() { _aspectDict = new(); }

        public void Add(string fileName)
        {
            Manual manual = new(fileName);
            foreach(var aspect in manual.GetAspects())
                Join(aspect);
        }


        new public string ToString()
        {
            string result = "";
            foreach(var fenData in _aspectDict)
            {
                result += fenData.Key + ' ';
                for(int i = 0;i < fenData.Value.Rank;i++)
                {
                    result += '[';
                    foreach(var dataValue in fenData.Value[i])
                    {
                        result += String.Format($"{dataValue.Key:x}:");
                        foreach(var x in dataValue.Value)
                            result += x.ToString();

                        result += ' ';
                        //result[result.Length - 1]
                    }
                    result += ']';
                }
                result += '\n';
            }

            return result;
        }

        private void Join(Aspect aspect)
        {
            string fen = aspect.Fen;
            int colorIndex = (int)aspect.Color;
            int data = aspect.Data;
            if(_aspectDict.ContainsKey(fen))
            {
                Dictionary<int, List<int>>[] aspectData = _aspectDict[fen];
                if(aspectData[colorIndex].ContainsKey(data))
                    aspectData[colorIndex][data][0]++;
                else
                    aspectData[colorIndex].Add(data, new() { 1 });
            }
            else
            {
                var aspectData = new Dictionary<int, List<int>>[2] { new(), new() };
                aspectData[colorIndex].Add(data, new() { 1 });
                _aspectDict.Add(fen, aspectData);
            }
        }

        private readonly Dictionary<string, Dictionary<int, List<int>>[]> _aspectDict;
    }
}
