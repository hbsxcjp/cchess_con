using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal enum PieceColor
    {
        RED,
        BLACK,
        NoColor = -1
    }

    internal enum PieceKind
    {
        KING,
        ADVISOR,
        BISHOP,
        KNIGHT,
        ROOK,
        CANNON,
        PAWN,
        NoKind = -1
    }

    abstract internal class Piece
    {
        public Piece(PieceColor color)
        {
            Color = color;
            Seat = Seat.NullSeat;
        }

        public PieceColor Color { get; }

        abstract public PieceKind Kind { get; }

        public Seat Seat { get; set; }

        public bool AtSeat { get { return Seat != Seat.NullSeat; } }
        abstract public char Char { get; }

        abstract public char Name { get; }

        virtual public char PrintName()
        {
            return Name;
        }

        virtual public List<Coord> PutCoord(bool isBottom)
        {
            return Seat.AllCoord();
        }

        // 可移动位置, 排除规则不允许行走的位置、排除同色的位置
        abstract public List<Coord> MoveCoord(Board board);

        new public string ToString
        {
            get { return (Color == PieceColor.RED ? "红" : "黑") + PrintName() + Char + Seat.Coord.ToString(); }
        }

        static public readonly Piece NullPiece = new NullPiece();

        static protected void AddColorCoord(List<Coord> coords, Board board, Coord coord, PieceColor color)
        {
            if(!board.IsColor(coord, color))
                coords.Add(coord);
        }

    }

    internal class PieceColFirstComp: IComparer<Piece>
    {
        public int Compare(Piece? x, Piece? y)
        {
            int xCol = x?.Seat.Col ?? 0, yCol = y?.Seat.Col ?? 0;
            int colComp = xCol.CompareTo(yCol);
            if(colComp != 0)
                return colComp;

            int xRow = x?.Seat.Row ?? 0, yRow = y?.Seat.Row ?? 0;
            return xRow.CompareTo(yRow);
        }
    }

    internal class King: Piece
    {
        public King(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.KING; }
        }

        override public char Char
        {
            get { return Color == PieceColor.RED ? 'K' : 'k'; }
        }

        override public char Name
        {
            get { return Color == PieceColor.RED ? '帅' : '将'; }
        }

        override public List<Coord> PutCoord(bool isBottom)
        {
            List<Coord> coords = new();
            int minRow = isBottom ? 0 : 7,
                maxRow = isBottom ? 2 : 9;
            for(int row = minRow;row <= maxRow;++row)
                for(int col = 3;col <= 5;++col)
                    coords.Add(new(row, col));

            return coords;
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            bool isBottom = Seat.IsBottom;
            int Row = Seat.Row,
                Col = Seat.Col;
            if(Col > 3)
                AddColorCoord(coords, board, new(Row, Col - 1), Color);
            if(Col < 5)
                AddColorCoord(coords, board, new(Row, Col + 1), Color);
            if(Row < (isBottom ? 2 : 9))
                AddColorCoord(coords, board, new(Row + 1, Col), Color);
            if(Row > (isBottom ? 0 : 7))
                AddColorCoord(coords, board, new(Row - 1, Col), Color);

            return coords;
        }
    }

    internal class Advisor: Piece
    {
        public Advisor(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.ADVISOR; }
        }

        override public char Char
        {
            get { return Color == PieceColor.RED ? 'A' : 'a'; }
        }

        override public char Name
        {
            get { return Color == PieceColor.RED ? '仕' : '士'; }
        }

        override public List<Coord> PutCoord(bool isBottom)
        {
            List<Coord> coords = new();
            int minRow = isBottom ? 0 : 7,
                maxRow = isBottom ? 2 : 9;

            for(int row = minRow;row <= maxRow;row += 2)
                for(int col = 3;col <= 5;col += 2)
                    coords.Add(new(row, col));

            coords.Add(new(minRow + 1, 4));
            return coords;
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            int Row = Seat.Row,
                Col = Seat.Col;
            if(Col != 4)
                AddColorCoord(coords, board, new(Seat.IsBottom ? 1 : 8, 4), Color);
            else
            {
                AddColorCoord(coords, board, new(Row - 1, Col - 1), Color);
                AddColorCoord(coords, board, new(Row - 1, Col + 1), Color);
                AddColorCoord(coords, board, new(Row + 1, Col - 1), Color);
                AddColorCoord(coords, board, new(Row + 1, Col + 1), Color);
            }

            return coords;
        }
    }

    internal class Bishop: Piece
    {
        public Bishop(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.BISHOP; }
        }

        override public char Char
        {
            get { return Color == PieceColor.RED ? 'B' : 'b'; }
        }

        override public char Name
        {
            get { return Color == PieceColor.RED ? '相' : '象'; }
        }

        override public List<Coord> PutCoord(bool isBottom)
        {
            List<Coord> coords = new();
            int minRow = isBottom ? 0 : 5,
                midRow = isBottom ? 2 : 7,
                maxRow = isBottom ? 4 : 9;
            for(int row = minRow;row <= maxRow;row += 4)
                for(int col = 2;col < Seat.ColNum;col += 4)
                    coords.Add(new(row, col));
            for(int col = 0;col < Seat.ColNum;col += 4)
                coords.Add(new(midRow, col));

            return coords;
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            bool isBottom = Seat.IsBottom;
            int Row = Seat.Row,
                Col = Seat.Col;
            int minRow = isBottom ? 0 : 5,
                midRow = isBottom ? 2 : 7,
                maxRow = isBottom ? 4 : 9;
            void AddCoord(int row, int col)
            {
                if(board[(row + Row) / 2, (col + Col) / 2].IsNull)
                    AddColorCoord(coords, board, new(row, col), Color);
            }

            if(Row < maxRow)
            {
                if(Col > 0)
                    AddCoord(Row + 2, Col - 2);
                if(Col < Seat.ColNum - 1)
                    AddCoord(Row + 2, Col + 2);
            }
            if(Row > 0)
            {
                if(Col > 0)
                    AddCoord(Row - 2, Col - 2);
                if(Col < Seat.ColNum - 1)
                    AddCoord(Row - 2, Col + 2);
            }

            return coords;
        }
    }

    internal class Knight: Piece
    {
        public Knight(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.KNIGHT; }
        }

        override public char Char
        {
            get { return Color == PieceColor.RED ? 'N' : 'n'; }
        }

        override public char Name
        {
            get { return '马'; }
        }

        override public char PrintName()
        {
            return Color == PieceColor.RED ? Name : '馬';
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            int Row = Seat.Row,
                Col = Seat.Col;
            Coord[,] allCoordLegs = new Coord[,]
            {
                {new(Row - 2, Col - 1), new(Row - 1, Col) } ,
                {new(Row - 2, Col + 1), new(Row - 1, Col)},
                {new(Row - 1, Col - 2), new(Row, Col - 1)},
                {new(Row - 1, Col + 2), new(Row, Col + 1)},
                {new(Row + 1, Col - 2), new(Row, Col - 1)},
                {new(Row + 1, Col + 2), new(Row, Col + 1)},
                {new(Row + 2, Col - 1), new(Row + 1, Col)},
                {new(Row + 2, Col + 1), new(Row + 1, Col)}
            };
            for(int i = 0;i < allCoordLegs.GetLength(0);++i)
            {
                var coord = allCoordLegs[i, 0];
                if(Seat.IsValid(coord) && board[allCoordLegs[i, 1]].IsNull)
                    AddColorCoord(coords, board, coord, Color);
            }

            return coords;
        }
    }

    internal class Rook: Piece
    {
        public Rook(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.ROOK; }
        }

        override public char Char
        {
            get { return Color == PieceColor.RED ? 'R' : 'r'; }
        }

        override public char Name
        {
            get { return '车'; }
        }

        override public char PrintName()
        {
            return Color == PieceColor.RED ? Name : '車';
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            int Row = Seat.Row,
                Col = Seat.Col;
            bool AddCoord(int row, int col)
            {
                AddColorCoord(coords, board, new(row, col), Color);
                return board[row, col].IsNull;
            }

            for(int r = Row - 1;r >= 0;--r)
                if(!AddCoord(r, Col))
                    break;

            for(int r = Row + 1;r < Seat.RowNum;++r)
                if(!AddCoord(r, Col))
                    break;

            for(int c = Col - 1;c >= 0;--c)
                if(!AddCoord(Row, c))
                    break;

            for(int c = Col + 1;c < Seat.ColNum;++c)
                if(!AddCoord(Row, c))
                    break;

            return coords;
        }
    }

    internal class Cannon: Piece
    {
        public Cannon(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.CANNON; }
        }

        override public char Char
        {
            get { return Color == PieceColor.RED ? 'C' : 'c'; }
        }

        override public char Name
        {
            get { return '炮'; }
        }

        override public char PrintName()
        {
            return Color == PieceColor.RED ? Name : '砲';
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            int Row = Seat.Row,
                Col = Seat.Col;
            bool skiped = false;
            bool AddCoordToBreak(int row,int col)
            {
                bool isNull = board[row, col].IsNull;
                if(!skiped)
                {
                    if(isNull)
                        AddColorCoord(coords, board, new(row, col), Color);
                    else
                        skiped = true;
                }
                else if(!isNull)
                {
                    AddColorCoord(coords, board, new(row, col), Color);
                    return true;
                }

                return false;
            }

            for(int r = Row - 1;r >= 0;--r)
                if(AddCoordToBreak(r, Col))
                    break;

            skiped = false;
            for(int r = Row + 1;r < Seat.RowNum;++r)
                if(AddCoordToBreak(r, Col))
                    break;

            skiped = false;
            for(int c = Col - 1;c >= 0;--c)
                if(AddCoordToBreak(Row, c))
                    break;

            skiped = false;
            for(int c = Col + 1;c < Seat.ColNum;++c)
                if(AddCoordToBreak(Row, c))
                    break;

            return coords;
        }
    }

    internal class Pawn: Piece
    {
        public Pawn(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.PAWN; }
        }

        override public char Char
        {
            get { return Color == PieceColor.RED ? 'P' : 'p'; }
        }

        override public char Name
        {
            get { return Color == PieceColor.RED ? '兵' : '卒'; }
        }

        override public List<Coord> PutCoord(bool isBottom)
        {
            List<Coord> coords = new();
            int minRow = isBottom ? 3 : 5,
                maxRow = isBottom ? 4 : 6;
            for(int row = minRow;row <= maxRow;++row)
                for(int col = 0;col < Seat.ColNum;col += 2)
                    coords.Add(new(row, col));

            minRow = isBottom ? 5 : 0;
            maxRow = isBottom ? 9 : 4;
            for(int row = minRow;row <= maxRow;++row)
                for(int col = 0;col < Seat.ColNum;++col)
                    coords.Add(new(row, col));

            return coords;
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            bool isBottom = Seat.IsBottom, bottomSide = board.BottomColor == Color;
            int Row = Seat.Row, Col = Seat.Col;
            // 已过河
            if(bottomSide != isBottom)
            {
                if(Col > 0)
                    AddColorCoord(coords, board, new(Row, Col - 1), Color);
                if(Col < Seat.ColNum - 1)
                    AddColorCoord(coords, board, new(Row, Col + 1), Color);
            }

            if(bottomSide && Row < Seat.RowNum - 1)
                AddColorCoord(coords, board, new(Row + 1, Col), Color);
            else if(!bottomSide && Row > 0)
                AddColorCoord(coords, board, new(Row - 1, Col), Color);

            return coords;
        }
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
            get { return '　'; }
        }

        new static public Seat Seat { get { return Seat.NullSeat; } }

        override public List<Coord> MoveCoord(Board board)
        {
            return new();
        }
    }
}
