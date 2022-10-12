using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    enum ChangeType
    {
        NoChange,
        SYMMETRY_V,
        ROTATE,
        SYMMETRY_H,
        EXCHANGE,
    }

    internal struct Coord
    {
        public Coord(int r, int c)
        {
            row = r;
            col = c;
        }
        public Coord(ushort data) : this(data >> 4, data & 0X0F)
        {
        }
        public Coord(string iccs) : this(Convert.ToInt32(iccs[1].ToString()),
            ColChars.IndexOf(iccs[0]))
        {
        }

        public ushort Data { get { return (ushort)(row << 4 | col); } }
        public string ICCSText() => string.Format($"{ColChars[col]}{row}");

        public Coord GetCoord(ChangeType ct)
        {
            if(ct == ChangeType.SYMMETRY_H)
                return new(row, Seat.SymmetryCol(col));
            else if(ct == ChangeType.SYMMETRY_V)
                return new(Seat.SymmetryRow(row), col);
            else if(ct == ChangeType.ROTATE)
                return new(Seat.SymmetryRow(row), Seat.SymmetryCol(col));

            return this;
        }

        new public string ToString() => string.Format($"({row},{col})");

        const string ColChars = "abcdefghi";

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
        public CoordPair(ushort data) : this(new((ushort)(data >> 8)), new((ushort)(data & 0XFF)))
        {
        }
        public CoordPair(string iccs) : this(new(iccs[..2]), new(iccs[2..]))
        {
        }

        public ushort Data { get { return (ushort)(FromCoord.Data << 8 | ToCoord.Data); } }
        public static CoordPair GetCoordPair(ushort data, ChangeType ct)
        {
            CoordPair coordPair = new(data);
            if(ct == ChangeType.NoChange)
                return coordPair;

            return new CoordPair(coordPair.FromCoord.GetCoord(ct), coordPair.ToCoord.GetCoord(ct));
        }

        public string ICCSText() => FromCoord.ICCSText() + ToCoord.ICCSText();
        public string DataText() => string.Format($"{Data:X4}");

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
        public static bool IsValid(Coord coord)
        {
            int row = coord.row, col = coord.col;
            return row >= 0 && row < RowNum && col >= 0 && col < ColNum;
        }

        public static List<Coord> AllCoord()
        {
            List<Coord> coords = new();
            for(int row = 0;row < Seat.RowNum;row++)
                for(int col = 0;col < Seat.ColNum;col++)
                    coords.Add(new(row, col));

            return coords;
        }

        public static int SymmetryRow(int row) => RowNum - 1 - row;
        public static int SymmetryCol(int col) => ColNum - 1 - col;

        public static readonly Seat NullSeat = new(new(-1, -1));

        public const int RowNum = 10;
        public const int ColNum = 9;

        private Piece _piece;
    }

}
