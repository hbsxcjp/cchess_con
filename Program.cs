//See https://aka.ms/new-console-template for more information
using cchess_con;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
        "飞相局【卒7进1】",
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
    //if(!File.Exists(path))
    //{
    //    // Create a file to write to.
    //    using(StreamWriter sw = File.CreateText(path))
    //    {
    //        sw.WriteLine("Hello");
    //        sw.WriteLine("And");
    //        sw.WriteLine("Welcome");
    //    }
    //}

    // Open the file to read from.
    //using(StreamReader sr = File.OpenText(path))
    //{
    //    string s;
    //    while((s = sr.ReadLine()) != null)
    //    {
    //        Console.WriteLine(s);
    //    }
    //}

    Stopwatch stopWatch = new();
    stopWatch.Restart();

    Board board = new();
    using StreamWriter sw = File.CreateText(path);
    sw.WriteLine(board.PiecesString());
    sw.WriteLine(board.PutCoordString());

    List<ChangeType> cts = new() {
        ChangeType.NoChange,
        ChangeType.SYMMETRY_V,
        ChangeType.ROTATE,
        ChangeType.SYMMETRY_H,
        ChangeType.EXCHANGE,
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

TestBoard();
TestManual();
TestAspect();
