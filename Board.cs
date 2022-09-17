using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
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

        public Seat this[int row, int col]
        {
            get
            {
                return _seats[row, col];
            }
            private set
            {
                _seats[row, col] = value;
            }
        }
        public Seat this[Coord coord]
        {
            get
            {
                return this[coord.row, coord.col];
            }
            private set
            {
                this[coord.row, coord.col] = value;
            }
        }

        public bool IsColor(Coord coord, PieceColor color)
        {
            return this[coord].Piece.Color == color;
        }
        public bool IsKilled(PieceColor color)
        {
            var otherColor = color == PieceColor.RED ? PieceColor.BLACK : PieceColor.RED;
            Seat kingSeat = GetKingSeat(color);
            int row = kingSeat.Row, col = kingSeat.Col;
            // 将帅是否对面
            bool isKingMeet()
            {
                var otherKingSeat = GetKingSeat(otherColor);
                if(col != otherKingSeat.Col)
                    return false;

                int otherRow = otherKingSeat.Row;
                int lowRow = Math.Min(row, otherRow),
                    upRow = Math.Max(row, otherRow);
                for(int r = lowRow + 1;r < upRow;++r)
                    if(!this[r, col].IsNull)
                        return false;

                return true;
            }

            // 某一方是否正在被将军
            bool isKilled()
            {
                Coord kingCoord = new(row, col);
                foreach(var piece in LivePieces(otherColor))
                    if(piece.MoveCoord(this).Contains(kingCoord))
                        return true;

                return false;
            }

            return isKingMeet() || isKilled();
        }
        public bool IsFailed(PieceColor color)
        {
            foreach(var piece in LivePieces(color))
                if(CanMoveCoord(piece.Seat.Coord).Count > 0)
                    return false;

            return true;
        }
        public Dictionary<Coord, List<Coord>> CanMoveCoord(PieceColor color)
        {
            Dictionary<Coord, List<Coord>> fromCoordToCoords = new();
            foreach(var piece in LivePieces(color))
            {
                var fromCoord = piece.Seat.Coord;
                //var coords = CanMoveCoord(fromCoord);
                //if(coords.Count > 0)
                //    fromCoordToCoords[fromCoord] = coords;
                fromCoordToCoords[fromCoord] = CanMoveCoord(fromCoord);
            }

            return fromCoordToCoords;
        }
        // 可移动位置, 排除将帅对面、被将军的位置
        public List<Coord> CanMoveCoord(Coord fromCoord)
        {
            List<Coord> coords = new();
            var fromSeat = this[fromCoord];
            if(fromSeat.IsNull)
                return coords;

            var color = fromSeat.Piece.Color;
            foreach(var toCoord in fromSeat.Piece.MoveCoord(this))
            {
                Seat toSeat = this[toCoord];
                Piece toPiece = toSeat.Piece;
                // 查询能走的位置时，如是对方将帅的位置则可走，不用判断是否被将军（因为判断是否被将军，会直接走棋吃子）
                if(toPiece.Kind == PieceKind.KING)
                    continue;

                fromSeat.MoveTo(toSeat, Piece.NullPiece);
                if(!IsKilled(color))
                    coords.Add(toCoord);
                toSeat.MoveTo(fromSeat, toPiece);
            }

            return coords;
        }
        public void Reset()
        {
            foreach(var seat in _seats)
                seat.SetNull();
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
                    var seat = this[row, col];
                    if(seat.IsNull)
                        num++;
                    else
                    {
                        if(num != 0)
                        {
                            line += string.Format($"{num}");
                            num = 0;
                        }
                        line += seat.Piece.Char;
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
                        if(piece.Char == ch && !piece.InSeat)
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
                        this[SymmetryRow(row), col++].Piece = getPiece(ch);
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
                    return this[seat.Row, SymmetryCol(seat.Col)];
                else if(ct == ChangeType.SYMMETRY_V)
                    return this[SymmetryRow(seat.Row), seat.Col];
                else if(ct == ChangeType.ROTATE)
                    return this[SymmetryRow(seat.Row), SymmetryCol(seat.Col)];

                return seat;
            }

            if(ct == ChangeType.EXCHANGE)
            {
                Dictionary<Seat, Piece> seatPieces = new();
                foreach(var seat in _seats)
                {
                    if(!seat.IsNull)
                        seatPieces.Add(seat, GetOtherPiece(seat.Piece));

                    seat.SetNull();
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
                        Seat seat = this[row, col];
                        Seat changedSeat = GetChangeSeat(seat, ct);
                        Piece piece = seat.Piece;
                        Piece changePiece = changedSeat.Piece;

                        changedSeat.SetNull(); // 切断联系
                        seat.Piece = changePiece;
                        changedSeat.Piece = piece;
                    }
            }

            return SetBottomColor();
        }

        public string GetZhStr(CoordPair coordPair)
        {
            string zhStr = "";
            Seat fromSeat = this[coordPair.FromCoord],
                toSeat = this[coordPair.ToCoord];
            if(fromSeat.IsNull)
                return zhStr;

            Piece fromPiece = fromSeat.Piece;
            PieceColor color = fromPiece.Color;
            PieceKind kind = fromPiece.Kind;
            char name = fromPiece.Name;
            int fromRow = fromSeat.Row, fromCol = fromSeat.Col,
                toRow = toSeat.Row, toCol = toSeat.Col;
            bool isSameRow = fromRow == toRow, isBottom = fromSeat.IsBottom;
            var pieces = LivePieces(color, kind, fromCol);
            int count = pieces.Count;
            if(count > 1 && kind > PieceKind.BISHOP)
            {
                if(kind == PieceKind.PAWN)
                    pieces = LivePieces(color, PieceKind.PAWN);

                pieces.Sort(new PieceColFirstComp());
                int index = pieces.IndexOf(fromPiece);
                if(isBottom)
                    index = count - 1 - index;

                zhStr = string.Format($"{PreChars(count)[index]}{name}");
            }
            else
            {  //将帅, 仕(士),相(象): 不用“前”和“后”区别，因为能退的一定在前，能进的一定在后
                char colChar = NumChars(color)[isBottom ? SymmetryCol(fromCol) : fromCol];
                zhStr = string.Format($"{name}{colChar}");
            }

            char movChar = ("退平进")[isSameRow ? 1 : (isBottom == toRow > fromRow ? 2 : 0)];
            char toNumColChar = !isSameRow && IsLinePiece(kind)
                ? NumChars(color)[Math.Abs(fromRow - toRow) - 1]
                : NumChars(color)[isBottom ? SymmetryCol(toCol) : toCol];
            zhStr += movChar + toNumColChar;
            //if(GetCoordPair(zhStr) != coordPair)
                //throw new Exception("GetCoordPair(zhStr) != coordPair ?");

            return zhStr;
        }
        public CoordPair GetCoordPair(string zhStr)
        {
            CoordPair coordPair = new();
            if(zhStr.Length != 4)
                return coordPair;

            PieceColor color = RedNumChars.Contains(zhStr[3]) ? PieceColor.RED : PieceColor.BLACK;
            bool isBottom = color == BottomColor;
            int index = 0,
                movDir = ("退平进".IndexOf(zhStr[2]) - 1) * (isBottom ? 1 : -1);

            List<Piece> pieces;
            PieceKind kind = GetKind(zhStr[0]);
            if(kind != PieceKind.NoKind)
            {   // 首字符为棋子名
                int fromCol = NumChars(color).IndexOf(zhStr[1]);
                if(isBottom)
                    fromCol = SymmetryCol(fromCol);

                pieces = LivePieces(color, kind, fromCol);
                if(pieces.Count <= 1)
                    throw new Exception("pieces.Count <= 1 ?");

                //# 排除：士、象同列时不分前后，以进、退区分棋子。移动方向为退时，修正index
                index = (pieces.Count == 2 && movDir == -1) ? 1 : 0; //&& isAdvBish(name)
            }
            else
            {
                kind = GetKind(zhStr[1]);
                pieces = LivePieces(color, kind);
                index = PreChars(pieces.Count).IndexOf(zhStr[0]);
                if(isBottom)
                    index = pieces.Count - 1 - index;
            }
            if(index == pieces.Count)
                throw new Exception("index == pieces.Count ?");
            pieces.Sort(new PieceColFirstComp());

            coordPair.FromCoord = pieces[index].Seat.Coord;
            int toNum = NumChars(color).IndexOf(zhStr[3]) + 1,
                toRow = coordPair.FromCoord.row,
                toCol = isBottom ? SymmetryCol(toNum - 1) : toNum - 1;
            if(IsLinePiece(kind))
            {
                if(movDir != 0)
                {
                    toRow += movDir * toNum;
                    toCol = coordPair.FromCoord.col;
                }
            }
            else
            {   // 斜线走子：仕、相、马
                int colAway = Math.Abs(toCol - coordPair.FromCoord.col);
                //  相距1或2列
                int rowInc = IsAdvisorBishop(kind) ? colAway : (colAway == 1 ? 2 : 1);
                toRow += movDir * rowInc;
            }
            coordPair.ToCoord = new(toRow, toCol);

            //if(GetZhStr(coordPair) != zhStr) throw new Exception("GetZhStr(coordPair) != zhStr ?");

            return coordPair;
        }

        public string PiecesString()
        {
            string result = "";
            foreach(var colorPieces in _pieces)
            {
                foreach(var kindPieces in colorPieces)
                {
                    foreach(var piece in kindPieces)
                        result += piece.ToString;

                    result += '\n';
                }

                result += '\n';
            }

            return result;
        }
        public string PutCoordString()
        {
            string result = "";
            foreach(var colorPieces in _pieces)
            {
                foreach(var kindPieces in colorPieces)
                {
                    foreach(var piece in kindPieces)
                    {
                        result += piece.ToString + " PutCoord: ";
                        int count = 0;
                        foreach(var coord in piece.PutCoord(piece.Color == BottomColor))
                        {
                            result += coord.ToString();
                            count++;
                        }

                        result += String.Format($"【{count}】\n", count);
                    }

                    result += '\n';
                }

                result += '\n';
            }

            return result;
        }
        public string CanMoveCoordString()
        {
            string result = "";
            int total = 0;
            foreach(var color in new List<PieceColor>() { PieceColor.RED, PieceColor.BLACK })
            {
                var fromCoordToCoords = CanMoveCoord(color);
                foreach(var coordToCoords in fromCoordToCoords)
                {
                    result += this[coordToCoords.Key].Piece.ToString + " CanMoveCoord: ";
                    int count = coordToCoords.Value.Count;
                    foreach(var coord in coordToCoords.Value)
                        result += coord.ToString();

                    result += String.Format($"【{count}】\n", count);
                    total += count;
                }
            }

            return result + String.Format($"总计：【{total}】\n", total);
        }

        public string ToString(bool hasEdge = true)
        {
            // 棋盘上边标识字符串
            string[] preStr =
            {
                @"　　　　　　　黑　方　　　　　　　
１　２　３　４　５　６　７　８　９
",
                @"　　　　　　　红　方　　　　　　　
一　二　三　四　五　六　七　八　九
"
            };

            // 文本空棋盘
            StringBuilder textBlankBoard =
                new(
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
"
                );
            // 边框粗线

            // 棋盘下边标识字符串
            string[] sufStr =
            {
                @"九　八　七　六　五　四　三　二　一
　　　　　　　红　方　　　　　　　
",
                @"９　８　７　６　５　４　３　２　１
　　　　　　　黑　方　　　　　　　
"
            };

            foreach(var seat in _seats)
                if(!seat.IsNull)
                {
                    int idx = SymmetryRow(seat.Row) * 2 * (Seat.ColNum * 2) + seat.Col * 2;
                    textBlankBoard[idx] = seat.Piece.PrintName();
                }

            if(!hasEdge)
                return textBlankBoard.ToString();

            int index = (int)BottomColor;
            return preStr[index] + textBlankBoard.ToString() + sufStr[index];
        }

        private List<Piece> LivePieces(PieceColor color)
        {
            return FilterPiece(delegate (Piece piece, PieceColor color, PieceKind kind, int col)
            {
                return piece.InSeat && piece.Color == color;
            },
                color, PieceKind.KING, 0);
        }
        private List<Piece> LivePieces(PieceColor color, PieceKind kind)
        {
            return FilterPiece(delegate (Piece piece, PieceColor color, PieceKind kind, int col)
            {
                return piece.InSeat && piece.Color == color && piece.Kind == kind;
            },
                color, kind, 0);
        }
        private List<Piece> LivePieces(PieceColor color, PieceKind kind, int col)
        {
            return FilterPiece(delegate (Piece piece, PieceColor color, PieceKind kind, int col)
            {
                return piece.InSeat && piece.Color == color && piece.Kind == kind && piece.Seat.Col == col;
            },
               color, kind, col);
        }

        private delegate bool CheckPiece(Piece piece, PieceColor color, PieceKind kind, int col);
        private List<Piece> FilterPiece(CheckPiece checkFun, PieceColor color, PieceKind kind, int col)
        {
            List<Piece> pieces = new();
            foreach(var colorPieces in _pieces)
                foreach(var kindPieces in colorPieces)
                    foreach(var piece in kindPieces)
                        if(checkFun(piece, color, kind, col))
                            pieces.Add(piece);

            return pieces;
        }

        private Seat GetKingSeat(PieceColor color) =>
            _pieces[(int)color][(int)PieceKind.KING][0].Seat;

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
                Type[] pieceType =
                {
                    typeof(King),
                    typeof(Advisor),
                    typeof(Bishop),
                    typeof(Knight),
                    typeof(Rook),
                    typeof(Cannon),
                    typeof(Pawn)
                };
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
            foreach(var coord in Seat.AllCoord())
                this[coord] = new(coord);
        }

        static private int SymmetryRow(int row) => Seat.RowNum - 1 - row;

        static private int SymmetryCol(int col) => Seat.ColNum - 1 - col;

        static private PieceKind GetKind(char name) => (PieceKind)("帅仕相马车炮兵将士象马车炮卒".IndexOf(name) % KindNum);
        static private bool IsLinePiece(PieceKind kind) => (kind == PieceKind.KING || kind == PieceKind.ROOK || kind == PieceKind.CANNON || kind == PieceKind.PAWN);
        static private bool IsAdvisorBishop(PieceKind kind) => (kind == PieceKind.ADVISOR || kind == PieceKind.BISHOP);
        static private string NumChars(PieceColor color) => color == PieceColor.RED ? RedNumChars : BlackNumChars;
        static private string PreChars(int count) => (count == 2 ? "前后" : (count == 3 ? "前中后" : "一二三四五"));

        private readonly Piece[][][] _pieces;
        private readonly Seat[,] _seats;

        private const char FENSplitChar = '/';
        private const string FEN = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR";

        private const string RedNumChars = "一二三四五六七八九";
        private const string BlackNumChars = "１２３４５６７８９";

        private const int ColorNum = 2;
        private const int KindNum = 7;
    }
}
