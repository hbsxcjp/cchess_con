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
        public Coord(char[] rowCol) : this(int.Parse(rowCol[0].ToString()), int.Parse(rowCol[1].ToString())) { }
        public Coord(string iccs) : this(int.Parse(iccs[1].ToString()), ColChars.IndexOf(iccs[0])) { }

        public string RowCol { get { return string.Format($"{row}{col}"); } }
        public string ICCS { get { return string.Format($"{ColChars[col]}{row}"); } }
        public bool IsBottom { get { return (row << 1) < RowCount; } }

        public static string GetRowCol(string rowCol, ChangeType ct)
        {
            int frow = int.Parse(rowCol[0].ToString()),
                fcol = int.Parse(rowCol[1].ToString()),
                trow = int.Parse(rowCol[2].ToString()),
                tcol = int.Parse(rowCol[3].ToString());
            void symmetryCol() { fcol = SymmetryCol(fcol); tcol = SymmetryCol(tcol); }
            void symmetryRow() { frow = SymmetryRow(frow); trow = SymmetryRow(trow); }
            switch(ct)
            {
                case ChangeType.Symmetry_H:
                    symmetryCol();
                    break;
                case ChangeType.Symmetry_V:
                    symmetryRow();
                    break;
                case ChangeType.Rotate:
                    symmetryCol();
                    symmetryRow();
                    break;
                default:
                    break;
            };

            return string.Format($"{frow}{fcol}{trow}{tcol}");
        }

        public static string RowCols(string iccses)
        {
            StringBuilder stringBuilder = new(iccses);
            for(int i = 0;i < stringBuilder.Length;++i)
                if(char.IsLetter(stringBuilder[i]))
                    stringBuilder[i] = ColChars.IndexOf(stringBuilder[i]).ToString()[0];

            return stringBuilder.ToString();
        }

        public static List<(int, int)> GetAllRowCol()
        {
            List<(int, int)> coords = new(RowCount * ColCount);
            for(int row = 0;row < RowCount;row++)
                for(int col = 0;col < ColCount;col++)
                    coords.Add((row, col));

            return coords;
        }

        public static int GetCol(int col, bool isBottomColor) => isBottomColor ? SymmetryCol(col) : col;
        public static int GetDoubleIndex(Coord coord) => SymmetryRow(coord.row) * 2 * (ColCount * 2) + coord.col * 2;
        public static bool IsValid(int row, int col) => row >= 0 && row < RowCount && col >= 0 && col < ColCount;

        public override string ToString() => string.Format($"({row},{col})");

        private static int SymmetryRow(int row) => RowCount - 1 - row;
        private static int SymmetryCol(int col) => ColCount - 1 - col;

        public const string ColChars = "ABCDEFGHI";

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
        public CoordPair(char[] rowCol) : this(new(rowCol[..2]), new(rowCol[2..])) { }
        public CoordPair(string iccs) : this(new(iccs[..2]), new(iccs[2..])) { }

        public Coord FromCoord { get; }
        public Coord ToCoord { get; }

        public string RowCol { get { return string.Format($"{FromCoord.RowCol}{ToCoord.RowCol}"); } }
        public string ICCS { get { return FromCoord.ICCS + ToCoord.ICCS; } }

        public override string ToString() => string.Format($"[{FromCoord},{ToCoord}]");
    }

    internal class Seat
    {
        public Seat(int row, int col)
        {
            Coord = new(row, col);
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
            foreach(var (row, col) in Coord.GetAllRowCol())
                seats[row, col] = new(row, col);

            return seats;
        }
        public override string ToString() => string.Format($"{Coord}:{_piece}");

        public static readonly Seat NullSeat = new(-1, -1);

        private Piece _piece;
    }

}
