using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal struct Coord
    {
        public Coord(int r, int c)
        {
            row = r;
            col = c;
        }

        new public string ToString() => string.Format($"({row},{col})");

        public int row;
        public int col;
    }

    internal struct CoordPair
    {
        public CoordPair(Coord fromCoord, Coord toCoord)
        {
            FromCoord = fromCoord;
            ToCoord = toCoord;
        }

        new public string ToString() => string.Format($"[{FromCoord.ToString()},{ToCoord.ToString()}]");

        public Coord FromCoord;
        public Coord ToCoord;
    }

    internal class Seat
    {
        public Seat(Coord coord)
        {
            Coord = coord;
            _piece = Piece.NullPiece;
        }

        public int Row { get { return Coord.row; } }
        public int Col { get { return Coord.col; } }
        public Coord Coord { get; }
        public Piece Piece
        {
            get { return _piece; }
            set
            {
                _piece.Seat = NullSeat;

                value.Seat = this;
                _piece = value;
            }
        }

        public void MoveTo(Seat toSeat, Piece fillPiece)
        {
            var piece = Piece;
            Piece = fillPiece; // 首先清空this与Piece的联系
            toSeat.Piece = piece; // 清空toSeat与ToPiece的联系
        }

        public bool IsNull { get { return Piece == Piece.NullPiece; } }
        public void SetNull() { Piece = Piece.NullPiece; }
        public bool IsBottom { get { return (Row << 1) < RowNum; } }
        static public bool IsValid(Coord coord)
        {
            int row = coord.row, col = coord.col;
            return row >= 0 && row < RowNum && col >= 0 && col < ColNum;
        }

        static public List<Coord> AllCoord()
        {
            List<Coord> coords = new();
            for(int row = 0;row < Seat.RowNum;row++)
                for(int col = 0;col < Seat.ColNum;col++)
                    coords.Add(new(row, col));

            return coords;
        }

        static public readonly Seat NullSeat = new(new(-1, -1));

        public const int RowNum = 10;
        public const int ColNum = 9;

        //private Coord _coord;
        private Piece _piece;
    }

}
