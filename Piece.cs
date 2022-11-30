namespace CChess;

internal enum PieceColor
{
    Red,
    Black,
    NoColor = -1
}

internal enum PieceKind
{
    King,
    Advisor,
    Bishop,
    Knight,
    Rook,
    Cannon,
    Pawn,
    NoKind = -1
}

internal abstract class Piece: IComparable
{
    public static readonly Piece Null = new NullPiece();
    public const char FENSplitChar = '/';

    private const string NameChars = "帅仕相马车炮兵将士象马车炮卒";
    private static readonly string[] NumChars = { "一二三四五六七八九", "１２３４５６７８９" };
    private const string PositionChars = "前中后";
    private const string MoveChars = "退平进";

    private const int ColorNum = 2;
    private const int KindNum = 7;

    public Piece(PieceColor color)
    {
        Color = color;
        Seat = Seat.Null;
    }

    public PieceColor Color { get; }
    public Seat Seat { get; set; }
    public Coord Coord { get { return Seat.Coord; } }

    abstract public PieceKind Kind { get; }
    abstract public char Char { get; }
    abstract public char Name { get; }

    virtual public char PrintName() => Name;

    virtual public List<Coord> PutCoord(Board board, bool isBottomColor) => board.GetAllCoords();

    // 可移动位置, 排除规则不允许行走的位置、排除同色的位置
    abstract public List<Coord> MoveCoord(Board board);

    public static Piece[][][] CreatPieces()
    {
        static Piece[] getKindPieces(PieceColor color, Type type, int num)
        {
            var kindPieces = new Piece[num];
            var constructorInfo = type.GetConstructor(new Type[] { typeof(PieceColor) });
            if(constructorInfo != null)
                for(int i = 0;i < num;i++)
                    kindPieces[i] = (Piece)constructorInfo.Invoke(new object[] { color });

            return kindPieces;
        }

        static Piece[][] getColorPieces(PieceColor color)
        {
            Type[] pieceType = { typeof(King), typeof(Advisor), typeof(Bishop),
                    typeof(Knight), typeof(Rook), typeof(Cannon), typeof(Pawn) };
            int[] KindNums = { 1, 2, 2, 2, 2, 2, 5 };
            Piece[][] pieces = new Piece[KindNum][];
            for(int k = 0;k < KindNum;k++)
                pieces[k] = getKindPieces(color, pieceType[k], KindNums[k]);

            return pieces;
        }

        var pieces = new Piece[ColorNum][][];
        for(int c = 0;c < ColorNum;c++)
            pieces[c] = getColorPieces((PieceColor)c);

        return pieces;
    }

    override public string ToString()
        => $"{(Color == PieceColor.Red ? "红" : (Color == PieceColor.Black ? "黑" : "无"))}{PrintName()}{Char}{Coord.ToString()}";

    protected List<Coord> GetCoords(Board board, List<(int row, int col)> rowCols)
        => rowCols.Select(rowCol => board[rowCol.row, rowCol.col].Coord).ToList();

    protected List<Coord> GetCoordsFilterSameColor(Board board, List<(int row, int col)> rowCols)
        => GetCoords(board, rowCols.Where(rowCol => board[rowCol.row, rowCol.col].Piece.Color != Color).ToList());

    public static int GetColorIndex(char ch) => char.IsUpper(ch) ? 0 : 1;
    public static int GetKindIndex(char ch) => ("KABNRCPkabnrcp".IndexOf(ch)) % KindNum;
    public static PieceKind GetKind(char name) => (PieceKind)(NameChars.IndexOf(name) % KindNum);
    public static bool IsLinePiece(PieceKind kind)
        => (kind == PieceKind.King || kind == PieceKind.Rook || kind == PieceKind.Cannon || kind == PieceKind.Pawn);
    public static char GetColChar(PieceColor color, int col) => NumChars[(int)color][col];
    public static int GetCol(PieceColor color, char colChar) => NumChars[(int)color].IndexOf(colChar);
    public static PieceColor GetColor(char numChar) => NumChars[0].Contains(numChar) ? PieceColor.Red : PieceColor.Black;
    public static string PreChars(int count) => (count == 2 ? "前后" : (count == 3 ? PositionChars : "一二三四五"));
    public static char MoveChar(bool isSameRow, bool isGo) => MoveChars[isSameRow ? 1 : (isGo ? 2 : 0)];
    public static int MoveDir(char movCh) => MoveChars.IndexOf(movCh) - 1;
    public static string PGNZHChars() => $"{NameChars}{NumChars[0]}{NumChars[1]}{PositionChars}{MoveChars}";

    int IComparable.CompareTo(object? obj)
    {
        if(obj is not Piece)
            return 0;

        return CompareTo((Piece)obj);
    }

    internal int CompareTo(Piece piece)
    {
        return Coord.CompareTo(piece.Coord);
    }

    internal class NullPiece: Piece
    {
        public NullPiece() : base(PieceColor.NoColor) { }

        override public PieceKind Kind
        {
            get { return PieceKind.NoKind; }
        }

        override public char Char
        {
            get { return '_'; }
        }

        override public char Name
        {
            get { return '空'; }
        }

        override public List<Coord> MoveCoord(Board board) => new();
    }

}

internal class King: Piece
{
    public King(PieceColor color) : base(color) { }

    override public PieceKind Kind { get { return PieceKind.King; } }

    override public char Char { get { return Color == PieceColor.Red ? 'K' : 'k'; } }

    override public char Name { get { return Color == PieceColor.Red ? '帅' : '将'; } }

    override public List<Coord> PutCoord(Board board, bool isBottom)
    {
        List<(int row, int col)> rowCols = new();
        int minRow = isBottom ? 0 : 7,
            maxRow = isBottom ? 2 : 9;
        for(int row = minRow;row <= maxRow;++row)
            for(int col = 3;col <= 5;++col)
                rowCols.Add((row, col));

        return GetCoords(board, rowCols);
    }

    override public List<Coord> MoveCoord(Board board)
    {
        List<(int row, int col)> rowCols = new();
        bool isBottom = Coord.IsBottom;
        int Row = Coord.row, Col = Coord.col;
        if(Col > 3)
            rowCols.Add((Row, Col - 1));
        if(Col < 5)
            rowCols.Add((Row, Col + 1));
        if(Row < (isBottom ? 2 : 9))
            rowCols.Add((Row + 1, Col));
        if(Row > (isBottom ? 0 : 7))
            rowCols.Add((Row - 1, Col));

        return GetCoordsFilterSameColor(board, rowCols);
    }
}

internal class Advisor: Piece
{
    public Advisor(PieceColor color) : base(color) { }

    override public PieceKind Kind { get { return PieceKind.Advisor; } }

    override public char Char { get { return Color == PieceColor.Red ? 'A' : 'a'; } }

    override public char Name { get { return Color == PieceColor.Red ? '仕' : '士'; } }

    override public List<Coord> PutCoord(Board board, bool isBottom)
    {
        List<(int row, int col)> rowCols = new();
        int minRow = isBottom ? 0 : 7,
            maxRow = isBottom ? 2 : 9;

        for(int row = minRow;row <= maxRow;row += 2)
            for(int col = 3;col <= 5;col += 2)
                rowCols.Add((row, col));

        rowCols.Add((minRow + 1, 4));
        return GetCoords(board, rowCols);
    }

    override public List<Coord> MoveCoord(Board board)
    {
        List<(int row, int col)> rowCols = new();
        bool isBottom = Coord.IsBottom;
        int Row = Coord.row, Col = Coord.col;
        if(Col != 4)
            rowCols.Add((isBottom ? 1 : 8, 4));
        else
        {
            rowCols.Add((Row - 1, Col - 1));
            rowCols.Add((Row - 1, Col + 1));
            rowCols.Add((Row + 1, Col - 1));
            rowCols.Add((Row + 1, Col + 1));
        }

        return GetCoordsFilterSameColor(board, rowCols);
    }
}

internal class Bishop: Piece
{
    public Bishop(PieceColor color) : base(color) { }

    override public PieceKind Kind { get { return PieceKind.Bishop; } }

    override public char Char { get { return Color == PieceColor.Red ? 'B' : 'b'; } }

    override public char Name { get { return Color == PieceColor.Red ? '相' : '象'; } }

    override public List<Coord> PutCoord(Board board, bool isBottom)
    {
        List<(int row, int col)> rowCols = new();
        int minRow = isBottom ? 0 : 5,
            midRow = isBottom ? 2 : 7,
            maxRow = isBottom ? 4 : 9;
        for(int row = minRow;row <= maxRow;row += 4)
            for(int col = 2;col < Coord.ColCount;col += 4)
                rowCols.Add((row, col));
        for(int col = 0;col < Coord.ColCount;col += 4)
            rowCols.Add((midRow, col));

        return GetCoords(board, rowCols);
    }

    override public List<Coord> MoveCoord(Board board)
    {
        List<(int row, int col)> rowCols = new();
        List<Coord> coords = new();
        bool isBottom = Coord.IsBottom;
        int Row = Coord.row, Col = Coord.col;
        int maxRow = isBottom ? (Coord.RowCount - 1) / 2 : Coord.RowCount - 1;
        void AddRowCol(int row, int col)
        {
            if(board[(row + Row) / 2, (col + Col) / 2].HasNullPiece)
                rowCols.Add((row, col));
        }

        if(Row < maxRow)
        {
            if(Col > 0)
                AddRowCol(Row + 2, Col - 2);
            if(Col < Coord.ColCount - 1)
                AddRowCol(Row + 2, Col + 2);
        }
        if(Row > 0)
        {
            if(Col > 0)
                AddRowCol(Row - 2, Col - 2);
            if(Col < Coord.ColCount - 1)
                AddRowCol(Row - 2, Col + 2);
        }

        return GetCoordsFilterSameColor(board, rowCols);
    }
}

internal class Knight: Piece
{
    public Knight(PieceColor color) : base(color) { }

    override public PieceKind Kind { get { return PieceKind.Knight; } }

    override public char Char { get { return Color == PieceColor.Red ? 'N' : 'n'; } }

    override public char Name { get { return '马'; } }

    override public char PrintName() { return Color == PieceColor.Red ? Name : '馬'; }

    override public List<Coord> MoveCoord(Board board)
    {
        List<(int row, int col)> rowCols = new();
        int Row = Coord.row, Col = Coord.col;
        ((int row, int col) to, (int row, int col) leg)[] allToLegRowCols =
        {
                ((Row - 2, Col - 1), (Row - 1, Col))  ,
                ((Row - 2, Col + 1), (Row - 1, Col)),
                ((Row - 1, Col - 2), (Row, Col - 1)),
                ((Row - 1, Col + 2), (Row, Col + 1)),
                ((Row + 1, Col - 2), (Row, Col - 1)),
                ((Row + 1, Col + 2), (Row, Col + 1)),
                ((Row + 2, Col - 1), (Row + 1, Col)),
                ((Row + 2, Col + 1), (Row + 1, Col))
            };
        foreach(var (to, leg) in allToLegRowCols)
        {
            if(Coord.IsValid(to.row, to.col) && (board[leg.row, leg.col].HasNullPiece))
                rowCols.Add((to.row, to.col));
        }

        return GetCoordsFilterSameColor(board, rowCols);
    }
}

internal class Rook: Piece
{
    public Rook(PieceColor color) : base(color) { }

    override public PieceKind Kind { get { return PieceKind.Rook; } }

    override public char Char { get { return Color == PieceColor.Red ? 'R' : 'r'; } }

    override public char Name { get { return '车'; } }

    override public char PrintName() { return Color == PieceColor.Red ? Name : '車'; }

    override public List<Coord> MoveCoord(Board board)
    {
        List<(int row, int col)> rowCols = new();
        int Row = Coord.row, Col = Coord.col;
        bool AddRowCol(int row, int col)
        {
            rowCols.Add((row, col));
            return board[row, col].HasNullPiece;
        }

        for(int r = Row - 1;r >= 0;--r)
            if(!AddRowCol(r, Col))
                break;

        for(int r = Row + 1;r < Coord.RowCount;++r)
            if(!AddRowCol(r, Col))
                break;

        for(int c = Col - 1;c >= 0;--c)
            if(!AddRowCol(Row, c))
                break;

        for(int c = Col + 1;c < Coord.ColCount;++c)
            if(!AddRowCol(Row, c))
                break;

        return GetCoordsFilterSameColor(board, rowCols);
    }
}

internal class Cannon: Piece
{
    public Cannon(PieceColor color) : base(color) { }

    override public PieceKind Kind { get { return PieceKind.Cannon; } }

    override public char Char { get { return Color == PieceColor.Red ? 'C' : 'c'; } }

    override public char Name { get { return '炮'; } }

    override public char PrintName() { return Color == PieceColor.Red ? Name : '砲'; }

    override public List<Coord> MoveCoord(Board board)
    {
        List<(int row, int col)> rowCols = new();
        int Row = Coord.row, Col = Coord.col;
        bool skiped = false;
        bool AddCoordToBreak(int row, int col)
        {
            bool isNull = board[row, col].HasNullPiece;
            if(!skiped)
            {
                if(isNull)
                    rowCols.Add((row, col));
                else
                    skiped = true;
            }
            else if(!isNull)
            {
                rowCols.Add((row, col));
                return true;
            }

            return false;
        }

        for(int r = Row - 1;r >= 0;--r)
            if(AddCoordToBreak(r, Col))
                break;

        skiped = false;
        for(int r = Row + 1;r < Coord.RowCount;++r)
            if(AddCoordToBreak(r, Col))
                break;

        skiped = false;
        for(int c = Col - 1;c >= 0;--c)
            if(AddCoordToBreak(Row, c))
                break;

        skiped = false;
        for(int c = Col + 1;c < Coord.ColCount;++c)
            if(AddCoordToBreak(Row, c))
                break;

        return GetCoordsFilterSameColor(board, rowCols);
    }
}

internal class Pawn: Piece
{
    public Pawn(PieceColor color) : base(color) { }

    override public PieceKind Kind { get { return PieceKind.Pawn; } }

    override public char Char { get { return Color == PieceColor.Red ? 'P' : 'p'; } }

    override public char Name { get { return Color == PieceColor.Red ? '兵' : '卒'; } }

    override public List<Coord> PutCoord(Board board, bool isBottom)
    {
        List<(int row, int col)> rowCols = new();
        int minRow = isBottom ? 3 : 5,
            maxRow = isBottom ? 4 : 6;
        for(int row = minRow;row <= maxRow;++row)
            for(int col = 0;col < Coord.ColCount;col += 2)
                rowCols.Add((row, col));

        minRow = isBottom ? 5 : 0;
        maxRow = isBottom ? 9 : 4;
        for(int row = minRow;row <= maxRow;++row)
            for(int col = 0;col < Coord.ColCount;++col)
                rowCols.Add((row, col));

        return GetCoords(board, rowCols);
    }

    override public List<Coord> MoveCoord(Board board)
    {
        List<(int row, int col)> rowCols = new();
        bool isBottom = Coord.IsBottom,
            isBottomColor = board.IsBottomColor(Color);
        int Row = Coord.row, Col = Coord.col;
        // 已过河
        if(isBottomColor != isBottom)
        {
            if(Col > 0)
                rowCols.Add((Row, Col - 1));
            if(Col < Coord.ColCount - 1)
                rowCols.Add((Row, Col + 1));
        }

        if(isBottomColor && Row < Coord.RowCount - 1)
            rowCols.Add((Row + 1, Col));
        else if(!isBottomColor && Row > 0)
            rowCols.Add((Row - 1, Col));

        return GetCoordsFilterSameColor(board, rowCols);
    }
}

