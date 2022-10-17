#define DEBUG

//See https://aka.ms/new-console-template for more information
using CChess;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

#if DEBUG
static void TestCoord()
{
    // Coord.ToString
    var allCoord = Coord.GetAllCoord();
    string expectCoordString = "(0,0)(0,1)(0,2)(0,3)(0,4)(0,5)(0,6)(0,7)(0,8)(1,0)(1,1)(1,2)(1,3)(1,4)(1,5)(1,6)(1,7)(1,8)(2,0)(2,1)(2,2)(2,3)(2,4)(2,5)(2,6)(2,7)(2,8)(3,0)(3,1)(3,2)(3,3)(3,4)(3,5)(3,6)(3,7)(3,8)(4,0)(4,1)(4,2)(4,3)(4,4)(4,5)(4,6)(4,7)(4,8)(5,0)(5,1)(5,2)(5,3)(5,4)(5,5)(5,6)(5,7)(5,8)(6,0)(6,1)(6,2)(6,3)(6,4)(6,5)(6,6)(6,7)(6,8)(7,0)(7,1)(7,2)(7,3)(7,4)(7,5)(7,6)(7,7)(7,8)(8,0)(8,1)(8,2)(8,3)(8,4)(8,5)(8,6)(8,7)(8,8)(9,0)(9,1)(9,2)(9,3)(9,4)(9,5)(9,6)(9,7)(9,8)【90】";
    string result = Utility.GetString(allCoord);
    Debug.Assert(expectCoordString == result);

    // Coord.Data
    string expectDataString = "0 1 2 3 4 5 6 7 8 16 17 18 19 20 21 22 23 24 32 33 34 35 36 37 38 39 40 48 49 50 51 52 53 54 55 56 64 65 66 67 68 69 70 71 72 80 81 82 83 84 85 86 87 88 96 97 98 99 100 101 102 103 104 112 113 114 115 116 117 118 119 120 128 129 130 131 132 133 134 135 136 144 145 146 147 148 149 150 151 152 【90】";
    List<ushort> allData = new();
    foreach(var coord in allCoord)
        allData.Add(coord.Data);
    result = Utility.GetString(allData, " ");
    Debug.Assert(expectDataString == result);

    List<Coord> dataCoords = new();
    foreach(var data in allData)
        dataCoords.Add(new(data));
    result = Utility.GetString(dataCoords);
    Debug.Assert(expectCoordString == result);

    // Coord.Iccs
    string expectIccsString = "a0 b0 c0 d0 e0 f0 g0 h0 i0 a1 b1 c1 d1 e1 f1 g1 h1 i1 a2 b2 c2 d2 e2 f2 g2 h2 i2 a3 b3 c3 d3 e3 f3 g3 h3 i3 a4 b4 c4 d4 e4 f4 g4 h4 i4 a5 b5 c5 d5 e5 f5 g5 h5 i5 a6 b6 c6 d6 e6 f6 g6 h6 i6 a7 b7 c7 d7 e7 f7 g7 h7 i7 a8 b8 c8 d8 e8 f8 g8 h8 i8 a9 b9 c9 d9 e9 f9 g9 h9 i9 【90】";
    List<string> allIccs = new();
    foreach(var coord in allCoord)
        allIccs.Add(coord.ICCS);
    result = Utility.GetString(allIccs, " ");
    Debug.Assert(expectIccsString == result);

    List<Coord> iccsCoords = new();
    foreach(var iccs in allIccs)
        iccsCoords.Add(new(iccs));
    result = Utility.GetString(iccsCoords);
    Debug.Assert(expectCoordString == result);

    // CoordComparer
    expectCoordString = "(9,8)(8,8)(7,8)(6,8)(5,8)(4,8)(3,8)(2,8)(1,8)(0,8)(9,7)(8,7)(7,7)(6,7)(5,7)(4,7)(3,7)(2,7)(1,7)(0,7)(9,6)(8,6)(7,6)(6,6)(5,6)(4,6)(3,6)(2,6)(1,6)(0,6)(9,5)(8,5)(7,5)(6,5)(5,5)(4,5)(3,5)(2,5)(1,5)(0,5)(9,4)(8,4)(7,4)(6,4)(5,4)(4,4)(3,4)(2,4)(1,4)(0,4)(9,3)(8,3)(7,3)(6,3)(5,3)(4,3)(3,3)(2,3)(1,3)(0,3)(9,2)(8,2)(7,2)(6,2)(5,2)(4,2)(3,2)(2,2)(1,2)(0,2)(9,1)(8,1)(7,1)(6,1)(5,1)(4,1)(3,1)(2,1)(1,1)(0,1)(9,0)(8,0)(7,0)(6,0)(5,0)(4,0)(3,0)(2,0)(1,0)(0,0)【90】";
    iccsCoords.Sort(new CoordComparer(true));
    result = Utility.GetString(iccsCoords);
    Debug.Assert(expectCoordString == result);
}

string TimeString(TimeSpan ts) => "RunTime " + String.Format("{0:00}:{1:00}:{2:00}.{3:00}\n",
        ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

const string output = @"C:\program1\gitee\cchess_cs\cchess_con\output\";

List<string> fileNames = new(){
        "01",
        "4四量拨千斤",
        "第09局",
        "布局陷阱--飞相局对金钩炮",
        "- 北京张强 (和) 上海胡荣华 (1993.4.27于南京)",

        //"中炮对屏风马",
        //"中炮【马8进7】",
        //"黑用开局库",
        //"仙人指路全集（史上最全最新版）",
        //"飞相局【卒7进1】",
        //"中炮【马2进3】"
    };
string[] extName = { ".xqf", ".cm", ".pgn" };

void TestBoard()
{
    string[] fens = {
        "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR",
        "5a3/4ak2r/6R2/8p/9/9/9/B4N2B/4K4/3c5",
        "5k3/9/9/9/9/9/4rp3/2R1C4/4K4/9",
    };

    string path = output + @"TestBoard.txt";

    Stopwatch stopWatch = new();
    stopWatch.Restart();

    Board board = new();
    using StreamWriter sw = File.CreateText(path);
    sw.WriteLine(board.PiecesString());
    sw.WriteLine(board.PutCoordString());

    List<ChangeType> cts = new() {
        ChangeType.NoChange,
        ChangeType.Symmetry_V,
        ChangeType.Rotate,
        ChangeType.Symmetry_H,
        ChangeType.Exchange,
    };
    foreach(string fen in fens)
    {
        board.SetFEN(fen);
        foreach(var ct in cts)
        {
            board.ChangeLayout(ct);
            sw.WriteLine(string.Format($"{ct}: \n{board.GetFEN()}"));
            sw.WriteLine(board.ToString());
            sw.WriteLine(board.CanMoveCoordString());
        }
    }

    stopWatch.Stop();
    sw.WriteLine(TimeString(stopWatch.Elapsed));
}

void TestManual()
{
    string path = output + @"TestManual.txt";

    Stopwatch stopWatch = new();
    stopWatch.Restart();

    int fromExtIndex = 1, toExtIndex = 2; // 0,1,2
    using StreamWriter sw = File.CreateText(path);
    foreach(string fileName in fileNames)
    {
        string theFileName = output + fileName;
        string fromFileName = theFileName + extName[fromExtIndex];
        string toFileName = theFileName + (fromExtIndex != toExtIndex ? "" : "-副本") + extName[toExtIndex];

        Manual manual = new(fromFileName);
        manual.Write(toFileName);
        sw.Write(fromFileName[(fromFileName.LastIndexOf('\\') + 1)..] + " => "
            + toFileName[(toFileName.LastIndexOf('\\') + 1)..] + "\n"
            + manual.ToString(true, true)); // true, true

        //foreach(var aspect in manual.GetAspects()) sw.WriteLine("aspect: " + aspect.ToString());
    }

    stopWatch.Stop();
    sw.WriteLine(TimeString(stopWatch.Elapsed));
}

void TestAspect()
{
    string path = output + @"TestAspect.txt";

    Stopwatch stopWatch = new();
    stopWatch.Restart();

    using StreamWriter sw = File.CreateText(path);
    Aspects aspects = new();
    foreach(string fileName in fileNames)
    {
        aspects.Add(output + fileName + extName[1]);
    }
    //sw.Write(aspects.ToString());

    string spFileName = output + @"Aspects.sp";
    aspects.Write(spFileName);

    Aspects aspects1 = new(spFileName);
    sw.Write(aspects1.ToString());


    stopWatch.Stop();
    sw.WriteLine(TimeString(stopWatch.Elapsed));
}

TestCoord();
TestBoard();
TestManual();
TestAspect();


#endif