//See https://aka.ms/new-console-template for more information
using cchess_con;

Console.WriteLine("Hello, World!");

//Piece king = new King(PieceColor.RED);
//Console.WriteLine(king.ToString() + king.Char + king.Name);

Board board = new Board();
Console.WriteLine(board.PiecesString());
Console.WriteLine(board.ShowString(true));
Console.WriteLine(board.GetFEN());
