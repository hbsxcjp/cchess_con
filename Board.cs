using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CChess
{
    internal class Board
    {
        public Board()
        {
            _seats = new Seat[Coord.RowCount, Coord.ColCount];
            _pieces = new Piece[ColorNum][][];

            InitPieces();
            InitSeats();
        }

        public PieceColor BottomColor { get; set; }
        public bool IsBottomColor(PieceColor color) => BottomColor == color;

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
            var otherColor = color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
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
                if(toPiece.Kind == PieceKind.King)
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
            for(int row = Coord.RowCount - 1;row >= 0;--row)
                fen += pieceChars[(row * Coord.ColCount)..((row + 1) * Coord.ColCount)] + FENSplitChar;

            return Regex.Replace(fen.Remove(fen.Length - 1), $"{Piece.NullPiece.Char}+",
                (Match match) => match.Value.Length.ToString());
        }
        public static string GetFEN(string fen, ChangeType ct)
        {
            if(ct == ChangeType.NoChange)
                return fen;

            if(ct == ChangeType.Exchange)
            {
                string resultFen = "";
                foreach(var ch in fen)
                    resultFen += char.IsLetter(ch) ? (char.IsLower(ch) ? char.ToUpper(ch) : char.ToLower(ch)) : ch;
                return resultFen;
            }

            string[] fenArray = fen.Split(FENSplitChar);
            if(fenArray.Length != Coord.RowCount)
                return fen;

            IEnumerable<string> result;
            IEnumerable<string> ReverseRow(IEnumerable<string> fenArray) => fenArray.Reverse();
            IEnumerable<string> ReverseCol(IEnumerable<string> fenArray)
            {
                List<string> lines = new();
                foreach(var line in fenArray)
                    lines.Add(string.Concat(line.Reverse()));
                return lines;
            }

            if(ct == ChangeType.Symmetry_H)
                result = ReverseCol(fenArray);
            else if(ct == ChangeType.Symmetry_V)
                result = ReverseRow(fenArray);
            else // if(ct == ChangeType.Rotate)
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
            if(fenArray.Length != Coord.RowCount)
                return false;

            Reset();
            int row = 0;
            foreach(var line in fenArray.Reverse())
            {
                int col = 0;
                foreach(char ch in line)
                    if(char.IsDigit(ch))
                        col += Convert.ToInt32(ch.ToString());
                    else
                        this[row, col++].Piece = GetNotAtSeatPiece(ch);

                row++;
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
            bool isSameRow = fromRow == toRow, isBottomColor = IsBottomColor(color);
            var pieces = LivePieces(color, kind, fromCol);
            if(pieces.Count > 1 && kind > PieceKind.Bishop)
            {
                // 有两条纵线，每条纵线上都有一个以上的兵
                if(kind == PieceKind.Pawn)
                    pieces = LivePieces_MultiColPawns(color);

                pieces.Sort(new PieceComparer());
                int index = pieces.IndexOf(fromPiece);
                if(isBottomColor)
                    index = pieces.Count - 1 - index;

                zhStr = string.Format($"{PreChars(pieces.Count)[index]}{name}");
            }
            else
            {  //将帅, 仕(士),相(象): 不用“前”和“后”区别，因为能退的一定在前，能进的一定在后
                char colChar = NumChars(color)[Coord.GetCol(fromCol, isBottomColor)];
                zhStr = string.Format($"{name}{colChar}");
            }

            char movChar = MoveChars[isSameRow ? 1 : (isBottomColor == toRow > fromRow ? 2 : 0)];
            char toNumColChar = !isSameRow && IsLinePiece(kind)
                ? NumChars(color)[Math.Abs(fromRow - toRow) - 1]
                : NumChars(color)[Coord.GetCol(toCol, isBottomColor)];
            zhStr += string.Format($"{movChar}{toNumColChar}");

            if(!GetCoordPair(zhStr).Equals(coordPair)) throw new Exception("GetCoordPair(zhStr) != coordPair ?");

            return zhStr;
        }
        public CoordPair GetCoordPair(string zhStr)
        {
            if(zhStr.Length != 4)
                return new();

            PieceColor color = RedNumChars.Contains(zhStr[3]) ? PieceColor.Red : PieceColor.Black;
            bool isBottomColor = IsBottomColor(color);
            int index, movDir = (MoveChars.IndexOf(zhStr[2]) - 1) * (isBottomColor ? 1 : -1);

            List<Piece> pieces;
            PieceKind kind = GetKind_name(zhStr[0]);
            if(kind != PieceKind.NoKind)
            {   // 首字符为棋子名
                pieces = LivePieces(color, kind, Coord.GetCol(NumChars(color).IndexOf(zhStr[1]), isBottomColor));
                if(pieces.Count == 0)
                    throw new Exception("pieces.Count == 0 ?");

                //# 排除：士、象同列时不分前后，以进、退区分棋子。移动方向为退时，修正index
                index = (pieces.Count == 2 && movDir == -1) ? 1 : 0; //&& isAdvBish(name)
            }
            else
            {
                kind = GetKind_name(zhStr[1]);
                pieces = kind == PieceKind.Pawn ? LivePieces_MultiColPawns(color) : LivePieces(color, kind);
                if(pieces.Count <= 1)
                    throw new Exception("pieces.Count <= 1 ?");

                index = PreChars(pieces.Count).IndexOf(zhStr[0]);
                if(isBottomColor)
                    index = pieces.Count - 1 - index;
            }
            if(index == pieces.Count)
                throw new Exception("index == pieces.Count ?");

            pieces.Sort(new PieceComparer());

            Coord fromCoord = pieces[index].Seat.Coord;
            int toNum = NumChars(color).IndexOf(zhStr[3]) + 1,
                toRow = fromCoord.row,
                toCol = Coord.GetCol(toNum - 1, isBottomColor);
            if(IsLinePiece(kind))
            {
                if(movDir != 0)
                {
                    toRow += movDir * toNum;
                    toCol = fromCoord.col;
                }
            }
            else
            {   // 斜线走子：仕、相、马
                int colAway = Math.Abs(toCol - fromCoord.col);
                //  相距1或2列
                int rowInc = (kind == PieceKind.Advisor || kind == PieceKind.Bishop)
                    ? colAway : (colAway == 1 ? 2 : 1);
                toRow += movDir * rowInc;
            }

            //if(GetZhStr(coordPair) != zhStr) throw new Exception("GetZhStr(coordPair) != zhStr ?");

            return new(fromCoord, new(toRow, toCol));
        }

        public string PiecesString()
        {
            string result = "";
            foreach(var colorPieces in _pieces)
            {
                foreach(var kindPieces in colorPieces)
                {
                    foreach(var piece in kindPieces)
                        result += piece.ToString();

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
                        result += piece.ToString() + " PutCoord: " + Utility.GetString(piece.PutCoord(IsBottomColor(piece.Color))) + "\n";

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
            foreach(var color in new List<PieceColor>() { PieceColor.Red, PieceColor.Black })
            {
                var fromCoordToCoords = CanMoveCoord(color);
                foreach(var coordToCoords in fromCoordToCoords)
                {
                    result += this[coordToCoords.Key].Piece.ToString() + " CanMoveCoord: ";
                    int count = coordToCoords.Value.Count;
                    foreach(var coord in coordToCoords.Value)
                        result += coord.ToString();

                    result += String.Format($"【{count}】\n", count);
                    total += count;
                }
            }

            return result + String.Format($"总计：【{total}】\n", total);
        }

        public static string PGNZHChars() => NameChars + RedNumChars + BlackNumChars + PositionChars + MoveChars;

        public override string ToString()
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
                    textBlankBoard[Coord.GetDoubleIndex(seat.Coord)] = seat.Piece.PrintName();

            int index = (int)BottomColor;
            return preStr[index] + textBlankBoard.ToString() + sufStr[index];
        }

        private List<Piece> LivePieces(PieceColor color)
        {
            return FilterPiece(delegate (Piece piece, PieceColor color, PieceKind kind, int col)
            {
                return piece.AtSeat && piece.Color == color;
            },
                color, PieceKind.King, 0);
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
        private List<Piece> LivePieces_MultiColPawns(PieceColor color)
        {
            List<Piece> pawnPieces = new();
            Dictionary<int, List<Piece>> colPieces = new();
            foreach(Piece piece in LivePieces(color, PieceKind.Pawn))
            {
                int col = piece.Seat.Col;
                if(!colPieces.ContainsKey(col))
                    colPieces[col] = new();

                colPieces[col].Add(piece);
            }

            foreach(var pieces in colPieces.Values)
                if(pieces.Count > 1)
                    pawnPieces.AddRange(pieces);

            return pawnPieces;
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
            _pieces[(int)color][(int)PieceKind.King][0].Seat;

        private bool SetBottomColor()
        {
            Seat kingSeat = GetKingSeat(PieceColor.Red);
            if(kingSeat.IsNull)
                return false;

            BottomColor = kingSeat.Coord.IsBottom ? PieceColor.Red : PieceColor.Black;
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
            foreach(var coord in Coord.GetAllCoord())
                this[coord] = new(coord);
        }

        private static int GetColorIndex(char ch) => char.IsUpper(ch) ? 0 : 1;
        private static int GetKindIndex(char ch) => ("KABNRCPkabnrcp".IndexOf(ch)) % KindNum;
        private static PieceKind GetKind_name(char name) => (PieceKind)(NameChars.IndexOf(name) % KindNum);
        private static bool IsLinePiece(PieceKind kind) => (kind == PieceKind.King || kind == PieceKind.Rook || kind == PieceKind.Cannon || kind == PieceKind.Pawn);
        private static string NumChars(PieceColor color) => color == PieceColor.Red ? RedNumChars : BlackNumChars;
        private static string PreChars(int count) => (count == 2 ? "前后" : (count == 3 ? PositionChars : "一二三四五"));

        // [Color][Kind][Index]
        private readonly Piece[][][] _pieces;
        // [row, col]
        private readonly Seat[,] _seats;

        private const char FENSplitChar = '/';

        private const string NameChars = "帅仕相马车炮兵将士象马车炮卒";
        private const string RedNumChars = "一二三四五六七八九";
        private const string BlackNumChars = "１２３４５６７８９";
        private const string PositionChars = "前中后";
        private const string MoveChars = "退平进";

        private const int ColorNum = 2;
        private const int KindNum = 7;
    }
}
