using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CChess
{
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

    internal abstract class Piece
    {
        public Piece(PieceColor color)
        {
            Color = color;
            Seat = null;
        }

        public PieceColor Color { get; }
        abstract public PieceKind Kind { get; }
        virtual public Seat? Seat { get; set; }
        abstract public char Char { get; }
        abstract public char Name { get; }

        virtual public char PrintName()
        {
            return Name;
        }

        virtual public List<Coord> PutCoord(bool isBottom)
        {
            return Coord.GetAllCoord();
        }

        // 可移动位置, 排除规则不允许行走的位置、排除同色的位置
        abstract public List<Coord> MoveCoord(Board board);

        override public string ToString()
            => (Color == PieceColor.Red ? "红" : "黑") + PrintName() + Char + Seat?.Coord.ToString();


        protected static void AddCoordDifColor(List<Coord> coords, Board board, Coord coord, PieceColor color)
        {
            if(board[coord].Piece.Color != color)
                coords.Add(coord);
        }

        public static readonly Piece NullPiece = new NullPiece();
    }

    internal class PieceComparer: IComparer<Piece>
    {
        public PieceComparer(bool isBottomColor) { _isBottomColor = isBottomColor; }
        public int Compare(Piece? x, Piece? y)
        {
            Seat? xseat = x?.Seat, yseat = y?.Seat;
            if(xseat != null && yseat != null)
                return new CoordComparer(_isBottomColor).Compare(xseat.Coord, yseat.Coord);

            return 0;
        }

        private readonly bool _isBottomColor;
    }

    internal class King: Piece
    {
        public King(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.King; }
        }

        override public char Char
        {
            get { return Color == PieceColor.Red ? 'K' : 'k'; }
        }

        override public char Name
        {
            get { return Color == PieceColor.Red ? '帅' : '将'; }
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
            Coord? coord = Seat?.Coord;
            if(coord == null)
                return coords;

            bool isBottom = coord.IsBottom;
            int Row = coord.row,
                Col = coord.col;
            if(Col > 3)
                AddCoordDifColor(coords, board, new(Row, Col - 1), Color);
            if(Col < 5)
                AddCoordDifColor(coords, board, new(Row, Col + 1), Color);
            if(Row < (isBottom ? 2 : 9))
                AddCoordDifColor(coords, board, new(Row + 1, Col), Color);
            if(Row > (isBottom ? 0 : 7))
                AddCoordDifColor(coords, board, new(Row - 1, Col), Color);

            return coords;
        }
    }

    internal class Advisor: Piece
    {
        public Advisor(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.Advisor; }
        }

        override public char Char
        {
            get { return Color == PieceColor.Red ? 'A' : 'a'; }
        }

        override public char Name
        {
            get { return Color == PieceColor.Red ? '仕' : '士'; }
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
            Coord? coord = Seat?.Coord;
            if(coord == null)
                return coords;

            bool isBottom = coord.IsBottom;
            int Row = coord.row,
                Col = coord.col;
            if(Col != 4)
                AddCoordDifColor(coords, board, new(isBottom ? 1 : 8, 4), Color);
            else
            {
                AddCoordDifColor(coords, board, new(Row - 1, Col - 1), Color);
                AddCoordDifColor(coords, board, new(Row - 1, Col + 1), Color);
                AddCoordDifColor(coords, board, new(Row + 1, Col - 1), Color);
                AddCoordDifColor(coords, board, new(Row + 1, Col + 1), Color);
            }

            return coords;
        }
    }

    internal class Bishop: Piece
    {
        public Bishop(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.Bishop; }
        }

        override public char Char
        {
            get { return Color == PieceColor.Red ? 'B' : 'b'; }
        }

        override public char Name
        {
            get { return Color == PieceColor.Red ? '相' : '象'; }
        }

        override public List<Coord> PutCoord(bool isBottom)
        {
            List<Coord> coords = new();
            int minRow = isBottom ? 0 : 5,
                midRow = isBottom ? 2 : 7,
                maxRow = isBottom ? 4 : 9;
            for(int row = minRow;row <= maxRow;row += 4)
                for(int col = 2;col < Coord.ColCount;col += 4)
                    coords.Add(new(row, col));
            for(int col = 0;col < Coord.ColCount;col += 4)
                coords.Add(new(midRow, col));

            return coords;
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            Coord? coord = Seat?.Coord;
            if(coord == null)
                return coords;

            bool isBottom = coord.IsBottom;
            int Row = coord.row,
                Col = coord.col;
            int maxRow = isBottom ? (Coord.RowCount - 1) / 2 : Coord.RowCount - 1;
            void AddCoord(int row, int col)
            {
                if(board[(row + Row) / 2, (col + Col) / 2].IsNull)
                    AddCoordDifColor(coords, board, new(row, col), Color);
            }

            if(Row < maxRow)
            {
                if(Col > 0)
                    AddCoord(Row + 2, Col - 2);
                if(Col < Coord.ColCount - 1)
                    AddCoord(Row + 2, Col + 2);
            }
            if(Row > 0)
            {
                if(Col > 0)
                    AddCoord(Row - 2, Col - 2);
                if(Col < Coord.ColCount - 1)
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
            get { return PieceKind.Knight; }
        }

        override public char Char
        {
            get { return Color == PieceColor.Red ? 'N' : 'n'; }
        }

        override public char Name
        {
            get { return '马'; }
        }

        override public char PrintName()
        {
            return Color == PieceColor.Red ? Name : '馬';
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            Coord? coord = Seat?.Coord;
            if(coord == null)
                return coords;

            int Row = coord.row,
                Col = coord.col;
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
                if(Coord.IsValid(to.row, to.col) && (board[leg.row, leg.col].IsNull))
                    AddCoordDifColor(coords, board, new(to.row, to.col), Color);
            }

            return coords;
        }
    }

    internal class Rook: Piece
    {
        public Rook(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.Rook; }
        }

        override public char Char
        {
            get { return Color == PieceColor.Red ? 'R' : 'r'; }
        }

        override public char Name
        {
            get { return '车'; }
        }

        override public char PrintName()
        {
            return Color == PieceColor.Red ? Name : '車';
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            Coord? coord = Seat?.Coord;
            if(coord == null)
                return coords;

            int Row = coord.row,
                Col = coord.col;
            bool AddCoord(int row, int col)
            {
                AddCoordDifColor(coords, board, new(row, col), Color);
                return board[row, col].IsNull;
            }

            for(int r = Row - 1;r >= 0;--r)
                if(!AddCoord(r, Col))
                    break;

            for(int r = Row + 1;r < Coord.RowCount;++r)
                if(!AddCoord(r, Col))
                    break;

            for(int c = Col - 1;c >= 0;--c)
                if(!AddCoord(Row, c))
                    break;

            for(int c = Col + 1;c < Coord.ColCount;++c)
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
            get { return PieceKind.Cannon; }
        }

        override public char Char
        {
            get { return Color == PieceColor.Red ? 'C' : 'c'; }
        }

        override public char Name
        {
            get { return '炮'; }
        }

        override public char PrintName()
        {
            return Color == PieceColor.Red ? Name : '砲';
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            Coord? coord = Seat?.Coord;
            if(coord == null)
                return coords;

            int Row = coord.row,
                Col = coord.col;
            bool skiped = false;
            bool AddCoordToBreak(int row, int col)
            {
                bool isNull = board[row, col].IsNull;
                if(!skiped)
                {
                    if(isNull)
                        AddCoordDifColor(coords, board, new(row, col), Color);
                    else
                        skiped = true;
                }
                else if(!isNull)
                {
                    AddCoordDifColor(coords, board, new(row, col), Color);
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

            return coords;
        }
    }

    internal class Pawn: Piece
    {
        public Pawn(PieceColor color) : base(color) { }

        override public PieceKind Kind
        {
            get { return PieceKind.Pawn; }
        }

        override public char Char
        {
            get { return Color == PieceColor.Red ? 'P' : 'p'; }
        }

        override public char Name
        {
            get { return Color == PieceColor.Red ? '兵' : '卒'; }
        }

        override public List<Coord> PutCoord(bool isBottom)
        {
            List<Coord> coords = new();
            int minRow = isBottom ? 3 : 5,
                maxRow = isBottom ? 4 : 6;
            for(int row = minRow;row <= maxRow;++row)
                for(int col = 0;col < Coord.ColCount;col += 2)
                    coords.Add(new(row, col));

            minRow = isBottom ? 5 : 0;
            maxRow = isBottom ? 9 : 4;
            for(int row = minRow;row <= maxRow;++row)
                for(int col = 0;col < Coord.ColCount;++col)
                    coords.Add(new(row, col));

            return coords;
        }

        override public List<Coord> MoveCoord(Board board)
        {
            List<Coord> coords = new();
            Coord? coord = Seat?.Coord;
            if(coord == null)
                return coords;

            bool isBottom = coord.IsBottom, 
                isBottomColor = board.IsBottomColor(Color);
            int Row = coord.row,
                Col = coord.col;
            // 已过河
            if(isBottomColor != isBottom)
            {
                if(Col > 0)
                    AddCoordDifColor(coords, board, new(Row, Col - 1), Color);
                if(Col < Coord.ColCount - 1)
                    AddCoordDifColor(coords, board, new(Row, Col + 1), Color);
            }

            if(isBottomColor && Row < Coord.RowCount - 1)
                AddCoordDifColor(coords, board, new(Row + 1, Col), Color);
            else if(!isBottomColor && Row > 0)
                AddCoordDifColor(coords, board, new(Row - 1, Col), Color);

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

        public override Seat? Seat { get { return null; } }

        override public List<Coord> MoveCoord(Board board)
        {
            return new();
        }
    }
}
