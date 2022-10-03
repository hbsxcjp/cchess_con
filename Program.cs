//See https://aka.ms/new-console-template for more information
using cchess_con;
using System.Diagnostics;

const string output = @"C:\program1\gitee\cchess_cs\cchess_con\output\";

static void TestBoard()
{
    string[] fens = {
        "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR",
        "5a3/4ak2r/6R2/8p/9/9/9/B4N2B/4K4/3c5",
        "5k3/9/9/9/9/9/4rp3/2R1C4/4K4/9"
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

    Board board = new();
    using StreamWriter sw = File.CreateText(path);
    sw.WriteLine(board.PiecesString());
    sw.WriteLine(board.PutCoordString());
    List<ChangeType> cts = new() {
            ChangeType.NoChange,
            ChangeType.EXCHANGE,
            ChangeType.ROTATE,
            ChangeType.SYMMETRY_H
        };
    foreach(string fen in fens)
    {
        board.SetFEN(fen);
        foreach(var ct in cts)
        {
            board.ChangeLayout(ct);
            sw.WriteLine(board.GetFEN());
            sw.WriteLine(board.ToString());
            sw.WriteLine(board.CanMoveCoordString());
        }
    }
}

static void TestManual()
{
    string[] fileNames = {
        "01.XQF",
        "4四量拨千斤.XQF",
        "第09局.XQF",
        "布局陷阱--飞相局对金钩炮.XQF",
        "- 北京张强 (和) 上海胡荣华 (1993.4.27于南京).xqf",

        //"中炮对屏风马.XQF",
        //"中炮【马8进7】.XQF",
        //"黑用开局库.XQF",
        //"仙人指路全集（史上最全最新版）.XQF",
        //"飞相局【卒7进1】.XQF",
        //"中炮【马2进3】.XQF"

        //"中炮对屏风马.cm",
        //"中炮【马8进7】.cm",
        //"黑用开局库.cm",
        //"仙人指路全集（史上最全最新版）.cm",
        //"飞相局【卒7进1】.cm",
        //"中炮【马2进3】.cm"
    };

    string path = output + @"TestManual.txt";

    Stopwatch stopWatch = new();
    stopWatch.Restart();
    //Thread.Sleep(10000);

    using StreamWriter sw = File.CreateText(path);
    foreach(string fileName in fileNames)
    {
        Manual manual = new(output + fileName);
        sw.WriteLine(fileName);
        sw.Write(manual.ToString()); // true

        //string cmFileName = output + fileName[..fileName.LastIndexOf('.')] + ".cm"; // 
        //string cmFileName = output + "副本-" + fileName[..fileName.LastIndexOf('.')] + ".cm"; // 
        //manual.Write(cmFileName);

        //Manual twoManual = new(cmFileName);
        //sw.Write(twoManual.ToString());
    }

    stopWatch.Stop();
    // Get the elapsed time as a TimeSpan value.
    TimeSpan ts = stopWatch.Elapsed;
    // Format and display the TimeSpan value.
    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}\n",
        ts.Hours, ts.Minutes, ts.Seconds,
        ts.Milliseconds / 10);
    sw.WriteLine("RunTime " + elapsedTime);
}

TestBoard();
TestManual();
