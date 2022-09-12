using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    enum ChangeType
    {
        EXCHANGE,
        ROTATE,
        SYMMETRY_H,
        SYMMETRY_V,
        NoChange
    }

    internal class Board
    {
        public Board()
        {
            _seats = new Seat[Seat.RowNum, Seat.ColNum];
            _pieces = new Piece[ColorNum][][];

            InitPieces();
            InitSeats();
        }

        public PieceColor BottomColor { get; set; }

        public Seat getSeat(int row, int col)
        {
            return _seats[row, col];
        }
        //public List<Seat> getALLSeat()
        //{
        //    List<Seat> seats = new();
        //    foreach(var seat in _seats)
        //        seats.Add(seat);
            
        //    return seats;
        //}

        public void Reset()
        {
            foreach(var seat in _seats)
                seat.Piece = Piece.NullPiece;
        }

        public string GetFEN()
        {
            string fen = "";
            for(int row = 0;row < Seat.RowNum;row++)
            {
                string line = "";
                int num = 0;
                for(int col = 0;col < Seat.ColNum;col++)
                {
                    var piece = _seats[row, col].Piece;
                    if(piece.IsNull)
                        num++;
                    else {
                        if(num != 0)
                        {
                            line += string.Format($"{num}");
                            num = 0;
                        }
                        line += piece.Char;
                    }
                }
                if(num != 0)
                    line += string.Format($"{num}");

                fen = (row == 0 ? line : line + FENSplitChar) + fen;
            }

            return fen;
        }

        public bool SetFEN(string fen)
        {
            Piece getPiece(char ch)
            {
                foreach(var kindPieces in _pieces[(int)(ch < 'a' ? PieceColor.RED : PieceColor.BLACK)])
                    foreach(var piece in kindPieces)
                        if(piece.Char == ch && piece.Seat.IsNull)
                            return piece;

                return Piece.NullPiece;
            }

            var fenArray = fen.Split(FENSplitChar);
            if(fenArray.Length != Seat.RowNum)
                return false;

            Reset();
            for(int row = 0;row < Seat.RowNum;row++)
            {
                int col = 0;
                foreach(char ch in fenArray[row])
                    if(ch.CompareTo('A') < 0)
                        col += ch - '0';
                    else
                        _seats[SymmetryRow(row), col++].Piece = getPiece(ch);
            }

            SetBottomColor();
            return true;
        }

        public bool ChangeLayout(ChangeType ct)
        {
            Piece GetOtherPiece(Piece piece)
            {
                var kindPieces = _pieces[(int)piece.Color][(int)piece.Kind];
                var otherKindPieces = _pieces[((int)piece.Color + 1) % 2][(int)piece.Kind];
                int index = 0;
                foreach(var pie in kindPieces)
                    if(pie == piece)
                        break;
                    else
                        index++;

                return otherKindPieces[index];
            }

            Seat GetChangeSeat(Seat seat, ChangeType ct)
            {
                if(ct == ChangeType.SYMMETRY_H)
                    return _seats[seat.Row, SymmetryCol(seat.Col)];
                else if(ct == ChangeType.SYMMETRY_V)
                    return _seats[SymmetryRow(seat.Row), seat.Col];
                else if(ct == ChangeType.ROTATE)
                    return _seats[SymmetryRow(seat.Row), SymmetryCol(seat.Col)];

                return seat;
            }

            if(ct == ChangeType.EXCHANGE)
            {
                Dictionary<Seat, Piece> seatPieces = new();
                foreach(var seat in _seats)
                {
                    if(!seat.Piece.IsNull)
                        seatPieces.Add(seat, GetOtherPiece(seat.Piece));

                    seat.Piece = Piece.NullPiece;
                }

                foreach(var seatPiece in seatPieces)
                    seatPiece.Key.Piece = seatPiece.Value;
            }
            else if(ct == ChangeType.SYMMETRY_H || ct == ChangeType.ROTATE)
            {
                int maxRow = ct == ChangeType.SYMMETRY_H ? Seat.RowNum : Seat.RowNum / 2,
                    maxCol = ct == ChangeType.SYMMETRY_H ? Seat.ColNum / 2 : Seat.ColNum;
                for(int row = 0;row < maxRow;++row)
                    for(int col = 0;col < maxCol;++col)
                    {
                        Seat seat = _seats[row, col];
                        Seat changedSeat = GetChangeSeat(seat, ct);
                        Piece piece = seat.Piece;
                        Piece changePiece = changedSeat.Piece;

                        changedSeat.Piece = Piece.NullPiece; // 切断联系
                        seat.Piece = changePiece;
                        changedSeat.Piece = piece;
                    }
            }

            return SetBottomColor();
        }

        public string PiecesString()
        {
            string result = "";
            foreach(var colorPieces in _pieces)
            {
                foreach(var kindPieces in colorPieces)
                {
                    foreach(var piece in kindPieces)
                        result += piece.ShowString;

                    result += '\n';
                }

                result += '\n';
            }

            return result;
        }

        public string ShowString(bool hasEdge)
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
            StringBuilder textBlankBoard = new(
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

            for(int r = 0;r < Seat.RowNum;r++)
            {
                for(int c = 0;c < Seat.ColNum;c++)
                {
                    var piece = _seats[r, c].Piece;
                    if(!piece.IsNull)
                    {
                        int idx = SymmetryRow(r) * 2 * (Seat.ColNum * 2) + c * 2;
                        textBlankBoard[idx] = piece.PrintName();
                    }
                }
            }

            if(!hasEdge)
                return textBlankBoard.ToString();

            int index = (int)BottomColor;
            return preStr[index] + textBlankBoard.ToString() + sufStr[index];
        }

        private List<Piece> LivePieces()
        {
            List<Piece> pieces = new();
            foreach(var colorPieces in _pieces)
                foreach(var kindPieces in colorPieces)
                    foreach(var piece in kindPieces)
                        if(!piece.Seat.IsNull)
                            pieces.Add(piece);

            return pieces;
        }
        private List<Piece> LivePieces(PieceColor color)
        {
            List<Piece> pieces = new();
            foreach(var piece in LivePieces())
                if(piece.Color == color)
                    pieces.Add(piece);

            return pieces;
        }
        private List<Piece> LivePieces(PieceColor color, PieceKind kind)
        {
            List<Piece> pieces = new();
            foreach(var piece in LivePieces(color))
                if(piece.Kind == kind)
                    pieces.Add(piece);

            return pieces;
        }
        private List<Piece> LivePieces(PieceColor color, char name)
        {
            return LivePieces(color, GetKind(name));
        }
        private List<Piece> LivePieces(PieceColor color, char name, int col)
        {
            List<Piece> pieces = new();
            foreach(var piece in LivePieces(color, name))
                if(piece.Seat.Col == col)
                    pieces.Add(piece);

            return pieces;
        }

        private List<Seat> LiveSeats_SortPawn(PieceColor color, bool isBottom)
        {
            // 最多5个兵, 按列、行建立映射，按列、行排序
            SortedDictionary<int, SortedDictionary<int, Seat>> colRowSeats = new();
            foreach(var seat in LiveSeats(LivePieces(color, PieceKind.PAWN)))
            {
                // 根据isBottom值排序
                int col = seat.Col, row = seat.Row;
                colRowSeats[isBottom ? -col : col][isBottom ? -row : row] = seat;
            }

            List<Seat> seats = new();
            foreach(var colSeats in colRowSeats.Values)
                // 只选取2个及以上的列
                if(colSeats.Count > 1)
                    seats.AddRange(colSeats.Values);

            return seats;
        }
        static private List<Seat> LiveSeats(List<Piece> livePieces)
        {
            List<Seat> seats = new();
            foreach(var piece in livePieces)
                seats.Add(piece.Seat);

            return seats;
        }

        private PieceKind GetKind(char name)
        {
            foreach(var kindPieces in _pieces[0])
                if(kindPieces[0].Name == name)
                    return kindPieces[0].Kind;

            return PieceKind.KING;
        }
        private Seat GetKingSeat(PieceColor color) => _pieces[(int)color][(int)PieceKind.KING][0].Seat;
        private bool SetBottomColor()
        {
            Seat kingSeat = GetKingSeat(PieceColor.RED);
            if(kingSeat.IsNull)
                return false;

            BottomColor = kingSeat.Row < Seat.RowNum / 2 ? PieceColor.RED : PieceColor.BLACK;
            return true;
        }

        private void InitPieces()
        {
            static Piece[] getKindPieces(PieceColor color, Type type, int num)
            {
                var kindPieces = new Piece[num];
                var constructorInfo = type.GetConstructor(new Type[] { typeof(PieceColor) });
                if(constructorInfo != null)
                {
                    for(int i = 0;i < num;i++)
                        kindPieces[i] = (Piece)constructorInfo.Invoke(new object[] { color });
                }

                return kindPieces;
            }

            static Piece[][] getColorPieces(PieceColor color)
            {
                Type[] pieceType = { typeof(King), typeof(Advisor), typeof(Bishop),
                    typeof(Knight), typeof(Rook), typeof(Cannon), typeof(Pawn) };
                int[] KindNums = { 1, 2, 2, 2, 2, 2, 5 };
                Piece[][] pieces = new Piece[KindNum][];
                for(int k = 0;k < KindNum;k++)
                    pieces[k] = getKindPieces(color, pieceType[k], KindNums[k]);

                return pieces;
            }

            for(int c = 0;c < ColorNum;c++)
                _pieces[c] = getColorPieces((PieceColor)c);
        }

        private void InitSeats()
        {
            foreach(var rowCol in Seat.AllCoord())
                _seats[rowCol.Key, rowCol.Value] = new(rowCol);
        }

        static private int SymmetryRow(int row) { return Seat.RowNum - 1 - row; }
        static private int SymmetryCol(int col) { return Seat.ColNum - 1 - col; }

        private readonly Piece[][][] _pieces;
        private readonly Seat[,] _seats;

        private const char FENSplitChar = '/';
        private const string FEN = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR";

        private const int ColorNum = 2;
        private const int KindNum = 7;

    }
}
