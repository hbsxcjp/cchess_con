using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CChess
{
    enum ChangeType
    {
        Exchange,
        Rotate,
        Symmetry_H,
        Symmetry_V,
        NoChange = -1,
    }

    internal class Coord
    {
        public Coord(int r, int c) { row = r; col = c; }
        public Coord(ushort data) : this(data >> 4, data & 0X0F) { }
        public Coord(string iccs) : this(int.Parse(iccs[1].ToString()), ColChars.IndexOf(iccs[0])) { }

        public ushort Data { get { return (ushort)(row << 4 | col); } }
        public string ICCS { get { return string.Format($"{ColChars[col]}{row}"); } }
        public bool IsBottom { get { return (row << 1) < RowCount; } }

        public Coord GetCoord(ChangeType ct)
        {
            return ct switch
            {
                ChangeType.Symmetry_H => new(row, SymmetryCol(col)),
                ChangeType.Symmetry_V => new(SymmetryRow(row), col),
                ChangeType.Rotate => new(SymmetryRow(row), SymmetryCol(col)),
                ChangeType.Exchange => this,
                ChangeType.NoChange => this,
                _ => this,
            };
        }

        public static List<Coord> GetAllCoord()
        {
            List<Coord> coords = new(RowCount * ColCount);
            for(int row = 0;row < RowCount;row++)
                for(int col = 0;col < ColCount;col++)
                    coords.Add(new(row, col));

            return coords;
        }

        public static int GetCol(int col, bool isBottomColor) => isBottomColor ? SymmetryCol(col) : col;
        public static int GetDoubleIndex(Coord coord) => SymmetryRow(coord.row) * 2 * (ColCount * 2) + coord.col * 2;
        public static bool IsValid(int row, int col) => row >= 0 && row < RowCount && col >= 0 && col < ColCount;

        public override string ToString() => string.Format($"({row},{col})");

        private static int SymmetryRow(int row) => RowCount - 1 - row;
        private static int SymmetryCol(int col) => ColCount - 1 - col;

        private const string ColChars = "abcdefghi";

        public const int RowCount = 10;
        public const int ColCount = 9;

        public readonly int row;
        public readonly int col;
    }

    internal class CoordComparer: IComparer<Coord>
    {
        public CoordComparer(bool isBottomColor) { _isBottomColor = isBottomColor; }
        public int Compare(Coord? x, Coord? y)
        {
            if(x == null || y == null)
                return 0;

            int colComp = x.col.CompareTo(y.col);
            if(colComp != 0)
                return _isBottomColor ? -colComp : colComp;

            int rowComp = x.row.CompareTo(y.row);
            return _isBottomColor ? -rowComp : rowComp;
        }

        private readonly bool _isBottomColor;
    }

    internal class CoordPair
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

        public Coord FromCoord { get; }
        public Coord ToCoord { get; }

        public ushort Data { get { return (ushort)(FromCoord.Data << 8 | ToCoord.Data); } }
        public string ICCS { get { return FromCoord.ICCS + ToCoord.ICCS; } }
        public string DataText { get { return string.Format($"{Data:X4}"); } }

        public CoordPair GetCoordPair(ChangeType ct) => new(FromCoord.GetCoord(ct), ToCoord.GetCoord(ct));

        public override string ToString() => string.Format($"[{FromCoord},{ToCoord}]");
    }

    internal class Seat
    {
        public Seat(Coord coord)
        {
            Coord = coord;
            _piece = Piece.NullPiece;
        }

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
        public bool IsNull { get { return this == NullSeat; } }
        public bool HasNullPiece { get { return Piece == Piece.NullPiece; } }

        public void MoveTo(Seat toSeat, Piece fromPiece)
        {
            Piece piece = Piece;
            Piece = fromPiece;
            toSeat.Piece = piece;
        }

        public static Seat[,] CreatSeats()
        {
            var seats = new Seat[Coord.RowCount, Coord.ColCount];
            foreach(var coord in Coord.GetAllCoord())
                seats[coord.row, coord.col] = new(coord);

            return seats;
        }
        public override string ToString() => string.Format($"{Coord}:{_piece}");

        public static readonly Seat NullSeat = new(new(-1, -1));

        private Piece _piece;
    }

}
