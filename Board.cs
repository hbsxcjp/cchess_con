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
        public Dictionary<Coord, List<Coord>> CanMoveCoord(PieceColor color, bool filterZero = false)
        {
            Dictionary<Coord, List<Coord>> fromCoordToCoords = new();
            foreach(var piece in LivePieces(color))
            {
                var fromCoord = piece.Seat.Coord;
                var coords = CanMoveCoord(fromCoord);
                if(coords.Count > 0 || !filterZero)
                    fromCoordToCoords[fromCoord] = coords;
            }

            return fromCoordToCoords;
        }

        // 可移动位置, 排除将帅对面、被将军的位置
        public List<Coord> CanMoveCoord(Coord fromCoord)
        {
            var fromSeat = this[fromCoord];
            if(fromSeat.IsNull)
                return new();

            var color = fromSeat.Piece.Color;
            List<Coord> coords = fromSeat.Piece.MoveCoord(this);
            coords.RemoveAll(toCoord =>
            {
                Seat toSeat = this[toCoord];
                Piece toPiece = toSeat.Piece;
                // 如是对方将帅的位置则直接可走，不用判断是否被将军（因为判断是否被将军，会直接走棋吃子）
                if(toPiece.Kind == PieceKind.KING)
                    return false;

                fromSeat.MoveTo(toSeat, Piece.NullPiece);
                bool killed = IsKilled(color);
                toSeat.MoveTo(fromSeat, toPiece);
                return killed;
            });

            return coords;
        }
        public void Reset()
        {
            foreach(var seat in _seats)
                seat.SetNull();
        }

        public string GetFEN()
        {
            string pieceChars = "";
            foreach(var seat in _seats)
                pieceChars += seat.Piece.Char;

            return GetFEN(pieceChars);
        }
        public static string GetFEN(string pieceChars)
        {
            string fen = "";
            if(pieceChars.Length != Seat.RowNum * Seat.ColNum)
                return fen;

            for(int row = 0;row < Seat.RowNum;row++)
            {
                string line = "";
                int num = 0, colIndex = row * Seat.ColNum;
                for(int col = 0;col < Seat.ColNum;col++)
                {
                    char ch = pieceChars[col + colIndex];
                    if(ch == Piece.NullPiece.Char)
                        num++;
                    else
                    {
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

                fen = (row == 0 ? line : line + FENSplitChar) + fen;
            }

            return fen;
        }
        public static string GetFEN(string fen, ChangeType ct)
        {
            if(ct == ChangeType.NoChange)
                return fen;

            if(ct == ChangeType.EXCHANGE)
            {
                string resultFen = "";
                foreach(var ch in fen)
                    resultFen += char.IsLetter(ch) ? (char.IsLower(ch) ? char.ToUpper(ch) : char.ToLower(ch)) : ch;
                return resultFen;
            }

            string[] fenArray = fen.Split(FENSplitChar);
            if(fenArray.Length != Seat.RowNum)
                return fen;

            IEnumerable<string> result;
            IEnumerable<string> ReverseRow(IEnumerable<string> fenArray) { return fenArray.Reverse(); }
            IEnumerable<string> ReverseCol(IEnumerable<string> fenArray)
            {
                List<string> lines = new();
                foreach(var line in fenArray)
                    lines.Add(string.Concat(line.Reverse()));
                return lines;
            }

            if(ct == ChangeType.SYMMETRY_H)
                result = ReverseCol(fenArray);
            else if(ct == ChangeType.SYMMETRY_V)
                result = ReverseRow(fenArray);
            else // if(ct == ChangeType.ROTATE)
                result = ReverseCol(ReverseRow(fenArray));

            return string.Join(FENSplitChar, result);
        }
        public bool SetFEN(string fen)
        {
            Piece GetNotAtSeatPiece(char ch)
            {
                foreach(var piece in _pieces[GetColorIndex(ch)][GetKindIndex(ch)])
                    if(!piece.AtSeat)
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
                    if(char.IsDigit(ch))
                        col += Convert.ToInt32(ch) - Convert.ToInt32('0');
                    else
                        this[Seat.SymmetryRow(row), col++].Piece = GetNotAtSeatPiece(ch);
            }

            SetBottomColor();
            return true;
        }
        public bool ChangeLayout(ChangeType ct) => SetFEN(GetFEN(GetFEN(), ct));

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
                char colChar = NumChars(color)[isBottom ? Seat.SymmetryCol(fromCol) : fromCol];
                zhStr = string.Format($"{name}{colChar}");
            }

            char movChar = ("退平进")[isSameRow ? 1 : (isBottom == toRow > fromRow ? 2 : 0)];
            char toNumColChar = !isSameRow && IsLinePiece(kind)
                ? NumChars(color)[Math.Abs(fromRow - toRow) - 1]
                : NumChars(color)[isBottom ? Seat.SymmetryCol(toCol) : toCol];
            zhStr += movChar + toNumColChar;

            if(GetCoordPair(zhStr).Equals(coordPair)) throw new Exception("GetCoordPair(zhStr) != coordPair ?");

            return zhStr;
        }
        public CoordPair GetCoordPair(string zhStr)
        {
            CoordPair coordPair = new();
            if(zhStr.Length != 4)
                return coordPair;

            PieceColor color = RedNumChars.Contains(zhStr[3]) ? PieceColor.RED : PieceColor.BLACK;
            bool isBottom = color == BottomColor;
            int index, movDir = ("退平进".IndexOf(zhStr[2]) - 1) * (isBottom ? 1 : -1);

            List<Piece> pieces;
            PieceKind kind = GetKind_name(zhStr[0]);
            if(kind != PieceKind.NoKind)
            {   // 首字符为棋子名
                int fromCol = NumChars(color).IndexOf(zhStr[1]);
                if(isBottom)
                    fromCol = Seat.SymmetryCol(fromCol);

                pieces = LivePieces(color, kind, fromCol);
                if(pieces.Count <= 1)
                    throw new Exception("pieces.Count <= 1 ?");

                //# 排除：士、象同列时不分前后，以进、退区分棋子。移动方向为退时，修正index
                index = (pieces.Count == 2 && movDir == -1) ? 1 : 0; //&& isAdvBish(name)
            }
            else
            {
                kind = GetKind_name(zhStr[1]);
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
                toCol = isBottom ? Seat.SymmetryCol(toNum - 1) : toNum - 1;
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
                int rowInc = (kind == PieceKind.ADVISOR || kind == PieceKind.BISHOP)
                    ? colAway : (colAway == 1 ? 2 : 1);
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

        public string ToString(bool showEdge = true)
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
                    int idx = Seat.SymmetryRow(seat.Row) * 2 * (Seat.ColNum * 2) + seat.Col * 2;
                    textBlankBoard[idx] = seat.Piece.PrintName();
                }

            if(!showEdge)
                return textBlankBoard.ToString();

            int index = (int)BottomColor;
            return preStr[index] + textBlankBoard.ToString() + sufStr[index];
        }

        private List<Piece> LivePieces(PieceColor color)
        {
            return FilterPiece(delegate (Piece piece, PieceColor color, PieceKind kind, int col)
            {
                return piece.AtSeat && piece.Color == color;
            },
                color, PieceKind.KING, 0);
        }
        private List<Piece> LivePieces(PieceColor color, PieceKind kind)
        {
            return FilterPiece(delegate (Piece piece, PieceColor color, PieceKind kind, int col)
            {
                return piece.AtSeat && piece.Color == color && piece.Kind == kind;
            },
                color, kind, 0);
        }
        private List<Piece> LivePieces(PieceColor color, PieceKind kind, int col)
        {
            return FilterPiece(delegate (Piece piece, PieceColor color, PieceKind kind, int col)
            {
                return piece.AtSeat && piece.Color == color && piece.Kind == kind && piece.Seat.Col == col;
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

        private static int GetColorIndex(char ch) => char.IsUpper(ch) ? 0 : 1;
        private static int GetKindIndex(char ch) => ("KABNRCPkabnrcp".IndexOf(ch)) % KindNum;
        private static PieceKind GetKind_name(char name) => (PieceKind)("帅仕相马车炮兵将士象马车炮卒".IndexOf(name) % KindNum);
        private static bool IsLinePiece(PieceKind kind) => (kind == PieceKind.KING || kind == PieceKind.ROOK || kind == PieceKind.CANNON || kind == PieceKind.PAWN);
        private static string NumChars(PieceColor color) => color == PieceColor.RED ? RedNumChars : BlackNumChars;
        private static string PreChars(int count) => (count == 2 ? "前后" : (count == 3 ? "前中后" : "一二三四五"));

        // [Color][Kind][Index]
        private readonly Piece[][][] _pieces;
        // [row, col]
        private readonly Seat[,] _seats;

        private const char FENSplitChar = '/';

        private const string RedNumChars = "一二三四五六七八九";
        private const string BlackNumChars = "１２３４５６７８９";

        private const int ColorNum = 2;
        private const int KindNum = 7;
    }
}
