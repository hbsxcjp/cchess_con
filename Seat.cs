using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal class Seat
    {
        public Seat(KeyValuePair<int, int> coord)
        {
            Row = coord.Key;
            Col = coord.Value;
            _piece = Piece.NullPiece;
        }

        public int Row { get; }
        public int Col { get; }
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
        public bool IsBottom { get { return Row << 1 < RowNum; } }
        static public bool IsValid(int row, int col)
        {
            return (row >= 0 && row < RowNum && col >= 0 && col < ColNum);
        }

        static public List<KeyValuePair<int, int>> AllCoord()
        {
            List<KeyValuePair<int, int>> coords = new();
            for(int row = 0;row < Seat.RowNum;row++)
                for(int col = 0;col < Seat.ColNum;col++)
                    coords.Add(new(row, col));

            return coords;
        }

        public void MoveTo(Seat toSeat, Piece fillPiece)
        {
            var piece = Piece;
            Piece = fillPiece; // 首先清空this与Piece的联系
            toSeat.Piece = piece; // 清空toSeat与ToPiece的联系
        }

        static public readonly Seat NullSeat = new(new(-1, -1));

        public const int RowNum = 10;
        public const int ColNum = 9;

        private Piece _piece;
    }

}
