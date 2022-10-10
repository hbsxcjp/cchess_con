<Query Kind="Statements">
  <RuntimeVersion>6.0</RuntimeVersion>
</Query>

string[] fens = {
        "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR",
        "5a3/4ak2r/6R2/8p/9/9/9/B4N2B/4K4/3c5",
        "5k3/9/9/9/9/9/4rp3/2R1C4/4K4/9",
    };
	
foreach(string fen in fens){
	string pieceChars = Regex.Replace(fen, '/'.ToString(), "");
	pieceChars = Regex.Replace(pieceChars, @"\d", (Match match) => new string('_', Convert.ToInt32(match.Value)));
	pieceChars.Dump();
	pieceChars.Length.Dump();
	
	int RowNum = 10, ColNum = 9;
    string exportFen = "";
    for(int row = 0;row < RowNum;++row)
    	exportFen += pieceChars[(row * ColNum)..((row + 1) * ColNum)] + '/';
		
	exportFen = Regex.Replace(exportFen.Remove(exportFen.Length - 1), "_+", (Match match) => match.Value.Length.ToString());
	fen.Dump();
	exportFen.Dump();
	(exportFen == fen).Dump();
}
