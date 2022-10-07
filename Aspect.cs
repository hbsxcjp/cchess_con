using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal class Aspects
    {
        public Aspects() { _aspectDict = new(); }
        public Aspects(string fileName) : this()
        {
            if(!File.Exists(fileName))
                return;

            using var stream = File.Open(fileName, FileMode.Open);
            using var reader = new BinaryReader(stream, Encoding.UTF8, false);
            int fenCount = reader.ReadInt32();
            for(int i = 0;i < fenCount;i++)
            {
                string fen = reader.ReadString();
                int dataCount = reader.ReadInt32();
                Dictionary<ushort, List<int>> aspectData = new();
                for(int j = 0;j < dataCount;j++)
                {
                    ushort data = reader.ReadUInt16();
                    int valueCount = reader.ReadInt32();
                    List<int> valueList = new();
                    for(int k = 0;k < valueCount;k++)
                        valueList.Add(reader.ReadInt32());

                    aspectData.TryAdd(data, valueList);
                }
                _aspectDict.TryAdd(fen, aspectData);
            }
        }
        public void Write(string fileName)
        {
            using var stream = File.Open(fileName, FileMode.Create);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, false);
            writer.Write(_aspectDict.Count);
            foreach(var fenData in _aspectDict)
            {
                writer.Write(fenData.Key);
                writer.Write(fenData.Value.Count);
                foreach(var aspectData in fenData.Value)
                {
                    writer.Write(aspectData.Key);
                    writer.Write(aspectData.Value.Count);
                    foreach(var x in aspectData.Value)
                        writer.Write(x);
                }
            }
        }

        public void Add(string fileName)
        {
            Manual manual = new(fileName);
            foreach(var aspect in manual.GetAspects())
                Join(aspect);

            // 以下同步方式与非同步方式的耗时基本相同，同步字典并行运行时被锁定？
            //var aspectList = (new Manual(fileName)).GetAspects();
            //Parallel.ForEach<(string fen, int data), bool>(
            //    aspectList,
            //    () => true,
            //    (aspect, loop, x) => Join(aspect),
            //    (x) => x = true);
        }

        public List<(CoordPair coordPair, List<int> valueList)>? GetAspectData(string fen)
        {
            var (finded, findCt, findFen) = FindCtFens(fen);
            if(!finded)
                return null;

            List<(CoordPair coordPair, List<int> valueList)> coordPairData = new();
            foreach(var dataValue in _aspectDict[findFen])
                coordPairData.Add((CoordPair.GetCoordPair(dataValue.Key, findCt), dataValue.Value));

            return coordPairData;
        }
        new public string ToString()
        {
            static string FenDataToString(KeyValuePair<string, Dictionary<ushort, List<int>>> fenData,
                  ParallelLoopState loop, string subString)
            {
                subString += fenData.Key + " [";
                foreach(var aspectData in fenData.Value)
                {
                    subString += String.Format($"{aspectData.Key:X4}(");
                    foreach(var x in aspectData.Value)
                        subString += x.ToString() + ' ';

                    subString = subString.TrimEnd() + ") ";
                }

                return subString.TrimEnd() + "]\n";
            }

            // 非常有效地提升了速度! 
            BlockingCollection<string> subStringCollection = new();
            Parallel.ForEach(
                _aspectDict,
                () => "",
                FenDataToString,
                (finalSubString) => subStringCollection.Add(finalSubString));

            return string.Concat(subStringCollection);
        }

        private bool Join((string fen, ushort data) aspect)
        {
            var (fen, data) = aspect;
            Dictionary<ushort, List<int>> aspectData;
            var (finded, findCt, findFen) = FindCtFens(fen);
            if(finded)
                aspectData = _aspectDict[findFen];
            else
            {
                aspectData = new();
                _aspectDict.Add(fen, aspectData);
            }

            if(findCt != ChangeType.NoChange)
                data = CoordPair.GetCoordPair(data, findCt).Data;

            if(aspectData.ContainsKey(data))
            {
                aspectData[data][0]++; // 第一项计数，列表可添加功能
            }
            else
                aspectData.TryAdd(data, new List<int>() { 1 });

            return true;
        }

        private (bool finded, ChangeType findCt, string findFen) FindCtFens(string fen)
        {
            bool finded = false;
            ChangeType findCt = ChangeType.NoChange;
            string findFen = "";
            foreach(var ct in new ChangeType[] {
                ChangeType.NoChange,
                ChangeType.SYMMETRY_V,
                ChangeType.ROTATE,
                ChangeType.SYMMETRY_H, })
            {
                var changeFen = Board.GetFEN(fen, ct);
                if(_aspectDict.ContainsKey(changeFen))
                {
                    finded = true;
                    findCt = ct;
                    findFen = changeFen;
                    if(ct != ChangeType.NoChange) Console.WriteLine($"\t\tfen: {fen}\n_aspectDict.ContainsKey: {changeFen}");
                    break;
                }
            }

            return (finded, findCt, findFen);
        }

        private readonly Dictionary<string, Dictionary<ushort, List<int>>> _aspectDict;
    }
}
