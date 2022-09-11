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
        public Seat(int row, int col)
        {
            Row = row;
            Col = col;
        }

        //public int RowCol { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public Piece? Piece { get; set; }

        public char Char { get { return Piece != null ? Piece.Char : NullChar; } }

        public const char NullChar = '_';
    }

}
