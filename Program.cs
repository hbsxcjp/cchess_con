//See https://aka.ms/new-console-template for more information
using cchess_con;
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

    Board board = new Board();
    using(StreamWriter sw = File.CreateText(path))
    {
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
}

static void TestManual()
{
    string[] fileNames = {
        "01.XQF",
        "4四量拨千斤.XQF",
        "第09局.XQF",
        "布局陷阱--飞相局对金钩炮.XQF",
        "- 北京张强 (和) 上海胡荣华 (1993.4.27于南京).xqf",
        //"中炮对屏风马.XQF"
    };

    string path = output + @"TestManual.txt";


    using(StreamWriter sw = File.CreateText(path))
    {
        foreach(string fileName in fileNames)
        {
            Manual manual = new(output + fileName);
            //sw.WriteLine(manual.ToString());


            string cmFileName = output + fileName + ".cm";
            manual.Write(cmFileName);

            Manual twoManual = new(cmFileName);
            sw.WriteLine(twoManual.ToString());

        }
    }
}

static void TestParallel()
{
    int[] nums = Enumerable.Range(0, 1000000).ToArray();
    long total = 0;

    // First type parameter is the type of the source elements
    // Second type parameter is the type of the thread-local variable (partition subtotal)
    Parallel.ForEach<int, long>(nums, // source collection
                                () => 0, // method to initialize the local variable
                                (j, loop, subtotal) => // method invoked by the loop on each iteration
                                {
                                    subtotal += j; //modify local variable
                                    return subtotal; // value to be passed to next iteration
                                },
                                // Method to be executed when each partition has completed.
                                // finalResult is the final value of subtotal for a particular partition.
                                (finalResult) => Interlocked.Add(ref total, finalResult)
                                );

    Console.WriteLine("The total from Parallel.ForEach is {0:N0}", total);
    // The example displays the following output:
    //        The total from Parallel.ForEach is 499,999,500,000


    //int[] nums = Enumerable.Range(0, 1000000).ToArray();
    total = 0;

    // Use type parameter to make subtotal a long, not an int
    Parallel.For<long>(0, nums.Length, () => 0, (j, loop, subtotal) =>
    {
        subtotal += nums[j];
        return subtotal;
    },
        (x) => Interlocked.Add(ref total, x)
    );

    Console.WriteLine("The total is {0:N0}", total);
}

TestBoard();
TestManual();

//TestParallel();