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

        public string? GetFEN()
        {
            string fen = "";
            for(int row = 0;row < RowNum;row++)
            {
                string line = "";
                int num = 0;
                for(int col = 0;col < ColNum;col++)
                {
                    char? ch = _seats?[row, col]?.Char;
                    if(ch == Seat.NullChar)
                        num++;
                    else {
                        if(num != 0)
                        {
                            line += string.Format($"{num}");
                            num = 0;
                        }
                        line += ch;
                    }
                }
                if(num != 0)
                    line += string.Format($"{num}");

                fen = line + FENSplitChar + fen;
            }

            return fen?.Remove(fen.Length - 1);
        }

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
                    if(ch.CompareTo('A') < 0)
                    {
                        col += ch - '0';
                        continue;
                    }

                    var seat = _seats?[SymmetryRow(row), col++];
                    if(seat == null) 
                        return false;

                    seat.Piece = getPiece(ch);
                }
            }

            return true;
        }


        static public int SymmetryRow(int row) { return RowNum - 1 - row; }
        static public int SymmetryCol(int col) { return ColNum - 1 - col; }

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

        public string? ShowString(bool hasEdge)
        {
            // 棋盘上边标识字符串
            string[] preStr = {
@"　　　　　　　黑　方　　　　　　　
１　２　３　４　５　６　７　８　９
",
@"　　　　　　　红　方　　　　　　　
一　二　三　四　五　六　七　八　九
"
            };

            // 文本空棋盘
            StringBuilder textBlankBoard = new StringBuilder(
@"┏━┯━┯━┯━┯━┯━┯━┯━┓
┃　│　│　│╲│╱│　│　│　┃
┠─┼─┼─┼─╳─┼─┼─┼─┨
┃　│　│　│╱│╲│　│　│　┃
┠─╬─┼─┼─┼─┼─┼─╬─┨
┃　│　│　│　│　│　│　│　┃
┠─┼─╬─┼─╬─┼─╬─┼─┨
┃　│　│　│　│　│　│　│　┃
┠─┴─┴─┴─┴─┴─┴─┴─┨
┃　　　　　　　　　　　　　　　┃
┠─┬─┬─┬─┬─┬─┬─┬─┨
┃　│　│　│　│　│　│　│　┃
┠─┼─╬─┼─╬─┼─╬─┼─┨
┃　│　│　│　│　│　│　│　┃
┠─╬─┼─┼─┼─┼─┼─╬─┨
┃　│　│　│╲│╱│　│　│　┃
┠─┼─┼─┼─╳─┼─┼─┼─┨
┃　│　│　│╱│╲│　│　│　┃
┗━┷━┷━┷━┷━┷━┷━┷━┛
");
            // 边框粗线

            // 棋盘下边标识字符串
            string[] sufStr = {
@"九　八　七　六　五　四　三　二　一
　　　　　　　红　方　　　　　　　
",
@"９　８　７　６　５　４　３　２　１
　　　　　　　黑　方　　　　　　　
"
    };

            for(int r = 0;r < RowNum;r++)
            {
                for(int c = 0;c < ColNum;c++)
                {
                    var seat = _seats?[r, c];
                    var piece = seat?.Piece;
                    if(piece != null)
                    {
                        int idx = SymmetryRow(r) * 2 * (ColNum * 2) + c * 2;
                        textBlankBoard[idx] = piece.PrintName();
                    }
                }
            }

            if(!hasEdge)
                return textBlankBoard.ToString();

            int index = (int)BottomColor;
            return preStr[index] + textBlankBoard.ToString() + sufStr[index];
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
