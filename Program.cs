//See https://aka.ms/new-console-template for more information
using cchess_con;

const string output = @"D:\8jchenjp\";

TestBoard();

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

    Board board = new Board();
    using(StreamWriter sw = File.CreateText(path))
    {
        sw.WriteLine(board.PiecesString());
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
                sw.WriteLine(board.ShowString(true));

            }
        }
    }
}

