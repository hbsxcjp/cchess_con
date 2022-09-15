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
        NoColor
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

        public bool IsNull
        {
            get { return this == NullPiece; }
        }

        public Seat Seat { get { return _seat; } set { _seat = value; } }

        abstract public char Char { get; }

        abstract public char Name { get; }

        virtual public char PrintName()
        {
            return Name;
        }

        virtual public List<KeyValuePair<int, int>> PutCoord(bool isBottom)
        {
            return Seat.AllCoord();
        }

        // 可移动位置, 排除将帅对面、被将军的位置
        public List<KeyValuePair<int, int>> CanMoveCoord(Board board)
        {
            var coords = MoveCoord(board);
            foreach(var coord in coords)
            {
                Seat toSeat = board.GetSeat(coord.Key, coord.Value);
                Piece toPiece = toSeat.Piece;

                Seat.MoveTo(toSeat, NullPiece);
                if(board.IsKingMeetKilled(Color))
                    coords.Remove(coord);
                toSeat.MoveTo(Seat, toPiece);
            }

            return coords;
        }
        // 可移动位置, 排除规则不允许行走的位置、排除同色的位置
        abstract public List<KeyValuePair<int, int>> MoveCoord(Board board);

        public string ShowString
        {
            get { return (Color == PieceColor.RED ? "红" : "黑") + PrintName() + Char; }
        }

        static public readonly Piece NullPiece = new NullPiece();

        static protected void AddNotColorCoord(
            List<KeyValuePair<int, int>> coords,
            Board board,
            int row,
            int col,
            PieceColor color
        )
        {
            if(board.GetSeat(row, col).Piece.Color != color)
                coords.Add(new(row, col));
        }

        private Seat _seat;
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
            int Row = Seat.Row,
                Col = Seat.Col;
            if(Col > 3)
                AddNotColorCoord(coords, board, Row, Col - 1, Color);
            if(Col < 5)
                AddNotColorCoord(coords, board, Row, Col + 1, Color);
            if(Row < (isBottom ? 2 : 9))
                AddNotColorCoord(coords, board, Row + 1, Col, Color);
            if(Row > (isBottom ? 0 : 7))
                AddNotColorCoord(coords, board, Row - 1, Col, Color);

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

        override public List<KeyValuePair<int, int>> PutCoord(bool isBottom)
        {
            List<KeyValuePair<int, int>> coords = new();
            int minRow = isBottom ? 0 : 7,
                maxRow = isBottom ? 2 : 9;
            
            for(int row = minRow;row <= maxRow;row += 2)
                for(int col = 3;col <= 5;col += 2)
                    coords.Add(new(row, col));

            coords.Add(new(minRow + 1, 4));
            return coords;
        }

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            int Row = Seat.Row,
                Col = Seat.Col;
            if(Col != 4)
                AddNotColorCoord(coords, board, Seat.IsBottom ? 1 : 8, 4, Color);
            else
            {
                AddNotColorCoord(coords, board, Row - 1, Col - 1, Color);
                AddNotColorCoord(coords, board, Row - 1, Col + 1, Color);
                AddNotColorCoord(coords, board, Row + 1, Col - 1, Color);
                AddNotColorCoord(coords, board, Row + 1, Col - 1, Color);
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
            int Row = Seat.Row,
                Col = Seat.Col;
            int minRow = IsBottom ? 0 : 5,
                midRow = IsBottom ? 2 : 7,
                maxRow = IsBottom ? 4 : 9;
            void AddCoord(int row, int col)
            {
                if(board.GetSeat((row + Row) / 2, (col + Col) / 2).IsNull)
                    AddNotColorCoord(coords, board, row, col, Color);
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

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            int Row = Seat.Row,
                Col = Seat.Col;
            int[,] allCoordLegs = new[,]
            {
                {Row - 2, Col - 1, Row - 1, Col } ,
                {Row - 2, Col + 1, Row - 1, Col},
                {Row - 1, Col - 2, Row, Col - 1},
                {Row - 1, Col + 2, Row, Col + 1},
                {Row + 1, Col - 2, Row, Col - 1},
                {Row + 1, Col + 2, Row, Col + 1},
                {Row + 2, Col - 1, Row + 1, Col},
                {Row + 2, Col + 1, Row + 1, Col}
            };
            for(int i = 0;i < allCoordLegs.GetLength(0);++i)
            {
                int row = allCoordLegs[i, 0], col = allCoordLegs[i, 1];
                if(Seat.IsValid(row, col) && board.GetSeat(allCoordLegs[i, 2], allCoordLegs[i, 3]).IsNull)
                    AddNotColorCoord(coords, board, row, col, Color);
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

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            int Row = Seat.Row,
                Col = Seat.Col;
            bool AddCoord(int row, int col)
            {
                AddNotColorCoord(coords, board, row, col, Color);
                return board.GetSeat(row, col).IsNull;
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

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            List<KeyValuePair<int, int>> coords = new();
            int Row = Seat.Row,
                Col = Seat.Col;
            bool skiped = false;
            bool AddCoordToBreak(int row, int col)
            {
                bool isNull = board.GetSeat(row, col).IsNull;
                if(!skiped)
                {
                    if(isNull)
                        AddNotColorCoord(coords, board, row, col, Color);
                    else
                        skiped = true;
                }
                else if(!isNull)
                {
                    AddNotColorCoord(coords, board, row, col, Color);
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
            int Row = Seat.Row,
                Col = Seat.Col;
            // 已过河
            if(IsBottom == Row > 4)
            {
                if(Col > 0)
                    AddNotColorCoord(coords, board, Row, Col - 1, Color);
                if(Col < Seat.ColNum)
                    AddNotColorCoord(coords, board, Row, Col + 1, Color);
            }

            if(IsBottom && Row < Seat.RowNum)
                AddNotColorCoord(coords, board, Row + 1, Col, Color);
            else if(!IsBottom && Row > 0)
                AddNotColorCoord(coords, board, Row - 1, Col, Color);

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

        override public List<KeyValuePair<int, int>> MoveCoord(Board board)
        {
            return new();
        }
    }
}
