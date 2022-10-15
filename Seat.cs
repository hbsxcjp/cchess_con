#define DEBUG

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

    internal struct Coord
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

        public static int GetCol(int col, bool isBottom) => isBottom ? SymmetryCol(col) : col;
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

#if DEBUG
        public static void Test()
        {
            // Coord.ToString
            var allCoord = GetAllCoord();
            string expectCoordString = "(0,0)(0,1)(0,2)(0,3)(0,4)(0,5)(0,6)(0,7)(0,8)(1,0)(1,1)(1,2)(1,3)(1,4)(1,5)(1,6)(1,7)(1,8)(2,0)(2,1)(2,2)(2,3)(2,4)(2,5)(2,6)(2,7)(2,8)(3,0)(3,1)(3,2)(3,3)(3,4)(3,5)(3,6)(3,7)(3,8)(4,0)(4,1)(4,2)(4,3)(4,4)(4,5)(4,6)(4,7)(4,8)(5,0)(5,1)(5,2)(5,3)(5,4)(5,5)(5,6)(5,7)(5,8)(6,0)(6,1)(6,2)(6,3)(6,4)(6,5)(6,6)(6,7)(6,8)(7,0)(7,1)(7,2)(7,3)(7,4)(7,5)(7,6)(7,7)(7,8)(8,0)(8,1)(8,2)(8,3)(8,4)(8,5)(8,6)(8,7)(8,8)(9,0)(9,1)(9,2)(9,3)(9,4)(9,5)(9,6)(9,7)(9,8)【90】";
            string result = Utility.GetString(allCoord);
            Debug.Assert(expectCoordString == result);

            // Coord.Data
            string expectDataString = "0 1 2 3 4 5 6 7 8 16 17 18 19 20 21 22 23 24 32 33 34 35 36 37 38 39 40 48 49 50 51 52 53 54 55 56 64 65 66 67 68 69 70 71 72 80 81 82 83 84 85 86 87 88 96 97 98 99 100 101 102 103 104 112 113 114 115 116 117 118 119 120 128 129 130 131 132 133 134 135 136 144 145 146 147 148 149 150 151 152 【90】";
            List<ushort> allData = new();
            foreach(var coord in allCoord)
                allData.Add(coord.Data);
            result = Utility.GetString(allData, " ");
            Debug.Assert(expectDataString == result);

            List<Coord> dataCoords = new();
            foreach(var data in allData)
                dataCoords.Add(new(data));
            result = Utility.GetString(dataCoords);
            Debug.Assert(expectCoordString == result);

            // Coord.Iccs
            string expectIccsString = "a0 b0 c0 d0 e0 f0 g0 h0 i0 a1 b1 c1 d1 e1 f1 g1 h1 i1 a2 b2 c2 d2 e2 f2 g2 h2 i2 a3 b3 c3 d3 e3 f3 g3 h3 i3 a4 b4 c4 d4 e4 f4 g4 h4 i4 a5 b5 c5 d5 e5 f5 g5 h5 i5 a6 b6 c6 d6 e6 f6 g6 h6 i6 a7 b7 c7 d7 e7 f7 g7 h7 i7 a8 b8 c8 d8 e8 f8 g8 h8 i8 a9 b9 c9 d9 e9 f9 g9 h9 i9 【90】";
            List<string> allIccs = new();
            foreach(var coord in allCoord)
                allIccs.Add(coord.ICCS);
            result = Utility.GetString(allIccs, " ");
            Debug.Assert(expectIccsString == result);

            List<Coord> iccsCoords = new();
            foreach(var iccs in allIccs)
                iccsCoords.Add(new(iccs));
            result = Utility.GetString(iccsCoords);
            Debug.Assert(expectCoordString == result);
        }
#endif
    }
    internal class CoordComparer: IComparer<Coord>
    {
        public int Compare(Coord x, Coord y)
        {
            int colComp = x.col.CompareTo(y.col);
            if(colComp != 0)
                return colComp;

            return x.row.CompareTo(y.row);
        }
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

        public CoordPair GetCoordPair(ChangeType ct) => new(FromCoord.GetCoord(ct), ToCoord.GetCoord(ct));

        public string ICCSText() => FromCoord.ICCS + ToCoord.ICCS;
        public string DataText() => string.Format($"{Data:X4}");

        override public string ToString() => string.Format($"[{FromCoord},{ToCoord}]");

        public readonly Coord FromCoord;
        public readonly Coord ToCoord;
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

        public static readonly Seat NullSeat = new(new(-1, -1));

        private Piece _piece;
    }

}
