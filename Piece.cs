using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    enum PieceColor
    {
        RED,
        BLACK,
        NoColor
    }

    enum PieceKind
    {
        KING,
        ADVISOR,    
        BISHOP,    
        KNIGHT,    
        ROOK,    
        CANNON,
        PAWN,
        NoKind
    }

    abstract internal class Piece
    {
        public Piece(PieceColor color)
        {
            Color = color;
            _seat = Seat.NullSeat;
        }

        public PieceColor Color { get; }

        abstract public PieceKind Kind { get; }

        public bool IsNull { get { return this == NullPiece; } }

        public Seat Seat { get { return _seat; } set { if(!IsNull) _seat = value; } }

        abstract public char Char { get; }

        abstract public char Name { get; }

        virtual public char PrintName() { return Name; }

        virtual public List<KeyValuePair<int, int>> PutCoord(bool isBottom) { return Seat.AllCoord(); }

        abstract public List<KeyValuePair<int, int>> MoveCoord(Board board);

        public string ShowString { get { return (Color == PieceColor.RED ? "红" : "黑") + PrintName() + Char; } }

        static public readonly Piece NullPiece = new NullPiece();

        private Seat _seat;
    }

    internal class King: Piece
    {
        public King(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.KING; } }

        override public char Char { get { return Color == PieceColor.RED ? 'K' : 'k'; } }

        override public char Name { get { return Color == PieceColor.RED ? '帅' : '将'; } }

        override public List<KeyValuePair<int, int>> PutCoord(bool isBottom)
        {
            List<KeyValuePair<int, int>> coords = new();
            int minRow = isBottom ? 0 : 7,
                maxRow = isBottom ? 2 : 9;
            for(int row = minRow;row <= maxRow;++row)
                for(int col = 3;col <= 5;++col)
                    coords.Add(new(row, col));

            return coords;
        }

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            bool isBottom = Seat.IsBottom;
            int Row = Seat.Row, Col = Seat.Col;
            if(Col > 3)
                coords.Add(new(Row, Col - 1));
            if(Col < 5)
                coords.Add(new(Row, Col + 1));
            if(Row < (isBottom ? 2 : 9))
                coords.Add(new(Row + 1, Col));
            if(Row > (isBottom ? 0 : 7))
                coords.Add(new(Row - 1, Col));

            return coords;
        }
    }

    internal class Advisor: Piece
    {
        public Advisor(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.ADVISOR; } }

        override public char Char { get { return Color == PieceColor.RED ? 'A' : 'a'; } }

        override public char Name { get { return Color == PieceColor.RED ? '仕' : '士'; } }

        override public List<KeyValuePair<int, int>> PutCoord(bool isBottom)
        {
            List<KeyValuePair<int, int>> coords = new();
            int minRow = isBottom ? 0 : 7,
                maxRow = isBottom ? 2 : 9;
            for(int row = minRow;row <= maxRow;row += 2)
                for(int col = 3;col <= 5;col += 2)
                    coords.Add(new(row, col));

            return coords;
        }

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            int Row = Seat.Row, Col = Seat.Col;
            if(Col != 4)
                coords.Add(new(Seat.IsBottom ? 1 : 8, 4));
            else
            {
                coords.Add(new(Row - 1, Col - 1));
                coords.Add(new(Row - 1, Col + 1));
                coords.Add(new(Row + 1, Col - 1));
                coords.Add(new(Row + 1, Col + 1));
            }

            return coords;
        }
    }

    internal class Bishop: Piece
    {
        public Bishop(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.BISHOP; } }

        override public char Char { get { return Color == PieceColor.RED ? 'B' : 'b'; } }

        override public char Name { get { return Color == PieceColor.RED ? '相' : '象'; } }

        override public List<KeyValuePair<int, int>> PutCoord(bool isBottom)
        {
            List<KeyValuePair<int, int>> coords = new();
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

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            bool IsBottom = Seat.IsBottom;
            int Row = Seat.Row, Col = Seat.Col;
            int minRow = IsBottom ? 0 : 5,
                midRow = IsBottom ? 2 : 7,
                maxRow = IsBottom ? 4 : 9;
            if(Row < maxRow)
            {
                if(Col > 0)
                    coords.Add(new(minRow + 2, Col - 2));
                if(Col < Seat.ColNum - 1)
                    coords.Add(new(minRow + 2, Col + 2));
            }
            else
            {
                coords.Add(new(maxRow - 2, Col - 2));
                coords.Add(new(maxRow - 2, Col + 2));
            }

            return coords;
        }
    }

    internal class Knight: Piece
    {
        public Knight(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.KNIGHT; } }

        override public char Char { get { return Color == PieceColor.RED ? 'N' : 'n'; } }

        override public char Name { get { return '马'; } }

        override public char PrintName() { return Color == PieceColor.RED ? Name : '馬'; }

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            int Row = Seat.Row, Col = Seat.Col;
            KeyValuePair<int, int>[] allCoords = {
                new(Row - 2, Col - 1), new(Row - 2, Col + 1),
                new(Row - 1, Col - 2), new(Row - 1, Col + 2),
                new(Row + 1, Col - 2), new(Row + 1, Col + 2),
                new(Row + 2, Col - 1), new(Row + 2, Col + 1)
            };
            foreach(var kvp in allCoords)
                if(kvp.Key >= 0 && kvp.Key < Seat.RowNum
                    && kvp.Value >= 0 && kvp.Value < Seat.ColNum)
                    coords.Add(kvp);

            return coords;
        }
    }

    internal class Rook: Piece
    {
        public Rook(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.ROOK; } }

        override public char Char { get { return Color == PieceColor.RED ? 'R' : 'r'; } }

        override public char Name { get { return '车'; } }

        override public char PrintName() { return Color == PieceColor.RED ? Name : '車'; }

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            bool IsBottom = Seat.IsBottom;
            int Row = Seat.Row, Col = Seat.Col;
            for(int i = 0;i < Row;i++)
            {
            }

            return coords;
        }
    }

    internal class Cannon: Piece
    {
        public Cannon(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.CANNON; } }

        override public char Char { get { return Color == PieceColor.RED ? 'C' : 'c'; } }

        override public char Name { get { return '炮'; } }

        override public char PrintName() { return Color == PieceColor.RED ? Name : '砲'; }

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            bool IsBottom = Seat.IsBottom;
            int Row = Seat.Row, Col = Seat.Col;
            for(int i = 0;i < Row;i++)
            {
            }

            return coords;
        }
    }

    internal class Pawn: Piece
    {
        public Pawn(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.PAWN; } }

        override public char Char { get { return Color == PieceColor.RED ? 'P' : 'p'; } }

        override public char Name { get { return Color == PieceColor.RED ? '兵' : '卒'; } }

        override public List<KeyValuePair<int, int>> PutCoord(bool isBottom)
        {
            List<KeyValuePair<int, int>> coords = new();
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

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            bool IsBottom = Seat.IsBottom;
            int Row = Seat.Row, Col = Seat.Col;
            // 已过河
            if(IsBottom == Row > 4)
            {
                if(Col > 0)
                    coords.Add(new(Row, Col - 1));
                if(Col < Seat.ColNum)
                    coords.Add(new(Row, Col + 1));
            }

            if(IsBottom && Row < Seat.RowNum)
                coords.Add(new(Row + 1, Col));
            else if(!IsBottom && Row > 0)
                coords.Add(new(Row - 1, Col));

            return coords;
        }
    }

    internal class NullPiece: Piece
    {
        public NullPiece() : base(PieceColor.NoColor) { }

        override public PieceKind Kind { get { return PieceKind.NoKind; } }

        override public char Char { get { return '_'; } }

        override public char Name { get { return '　'; } }

        override public List<KeyValuePair<int, int>> MoveCoord(Board board) { return new(); }
    }
}
