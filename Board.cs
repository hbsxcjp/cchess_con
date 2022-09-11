using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal class Board
    {
        public const int RowNum = 10;
        public const int ColNum = 9;

        public Board()
        {
            InitPieces();
            InitSeats();
            SetFEN(FEN);
        }

        public PieceColor BottomColor { get; set; }

        public bool SetFEN(string fen)
        {
            var fenArray = fen.Split(FENSplitChar);
            if(fenArray.Length != RowNum)
                return false;

            Piece? getPiece(char ch)
            {
                var colorPieces = _pieces?[(int)(ch < 'a' ? PieceColor.RED : PieceColor.BLACK)];
                if(colorPieces == null)
                    return null;

                foreach(var kindPieces in colorPieces)
                {
                    foreach(var piece in kindPieces)
                    {
                        if(piece?.Char == ch && piece.Seat == null)
                            return piece;
                    }

                }

                return null;
            }

            for(int row = 0;row < fenArray.Length;row++)
            {
                int col = 0;
                foreach(char ch in fenArray[row])
                {
                    var seat = _seats?[row, col++];
                    if(seat == null) 
                        return false;

                    seat.Piece = getPiece(ch);
                }

            }

            return true;
        }


        //static public int SymmetryRow(int row) { return RowNum - 1 - row; }
        //static public int SymmetryCol(int col) { return ColNum - 1 - col; }

        public string PiecesString()
        {
            string result = "";
            for(int i = 0;i < _pieces?.Length;i++)
            {
                Piece?[][]? colorPiecees = _pieces[i];
                foreach(var kindPieces in colorPiecees)
                {
                    foreach(var piece in kindPieces)
                    {
                        result += piece?.ShowString;
                    }

                    result += '\n';
                }

                result += '\n';
            }

            return result;
        }

        public string SeatsString()
        {
            string result = "";
            for(int r = 0;r < RowNum;r++)
            {
                for(int c = 0;c < ColNum;c++)
                    result += _seats?[r, c]?.Name;

                result += '\n';
            }

            return result;
        }

        private void InitPieces()
        {
            const int ColorNum = 2;
            const int KindNum = 7;
            int[] KindNums = { 1, 2, 2, 2, 2, 2, 5 };
            _pieces = new Piece?[ColorNum][][];
            for(int c = 0;c < ColorNum;c++)
            {
                PieceColor color = (PieceColor)c;
                var colorKindPieces = new Piece?[KindNum][];
                for(int k = 0;k < KindNum;k++)
                {
                    int num = KindNums[k];
                    var kindPieces = new Piece?[num];
                    switch((PieceKind)k)
                    {
                        case PieceKind.KING:
                            kindPieces[0] = new King(color);
                            break;
                        case PieceKind.ADVISOR:
                            for(int i = 0;i < num;i++)
                            {
                                kindPieces[i] = new Advisor(color);
                            }
                            break;
                        case PieceKind.BISHOP:
                            for(int i = 0;i < num;i++)
                            {
                                kindPieces[i] = new Bishop(color);
                            }
                            break;
                        case PieceKind.KNIGHT:
                            for(int i = 0;i < num;i++)
                            {
                                kindPieces[i] = new Knight(color);
                            }
                            break;
                        case PieceKind.ROOK:
                            for(int i = 0;i < num;i++)
                            {
                                kindPieces[i] = new Rook(color);
                            }
                            break;
                        case PieceKind.CANNON:
                            for(int i = 0;i < num;i++)
                            {
                                kindPieces[i] = new Cannon(color);
                            }
                            break;
                        default: //PieceKind.PAWN:
                            for(int i = 0;i < num;i++)
                            {
                                kindPieces[i] = new Pawn(color);
                            }
                            break;
                    }

                    colorKindPieces[k] = kindPieces;
                }

                _pieces[c] = colorKindPieces;
            }
        }

        private void InitSeats()
        {
            _seats = new Seat[RowNum, ColNum];
            for(int row = 0;row < RowNum;row++)
            {
                for(int col = 0;col < ColNum;col++)
                {
                    _seats[row, col] = new Seat(row, col);
                }
            }
        }

        private Piece?[][][]? _pieces;
        private Seat?[,]? _seats;

        private const string FEN = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR";
        private const char FENSplitChar = '/';


    }
}
