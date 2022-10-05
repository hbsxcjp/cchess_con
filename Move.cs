using cchess_con;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal enum VisibleType
    {
        ALL,
        TRUE,
        FALSE
    }

    internal class Move
    {
        public Move(bool visible = true)
        {
            Before = null;
            ToPiece = Piece.NullPiece;

            Visible = visible;
            _afterMoves = null;
        }
        public Move(CoordPair coordPair, string? remark = null, bool visible = true) : this(visible)
        {
            CoordPair = coordPair;
            Remark = remark;
        }

        public static Move CreateRootMove() { return new(); }

        public int Id { get; set; }
        public Move? Before { get; set; }
        public CoordPair CoordPair { get; set; }
        public string? Remark { get; set; }
        public bool Visible { get; set; }
        public int BeforeNum
        {
            get
            {
                int count = 0;
                Move move = this;
                while(move.Before != null)
                {
                    count++;
                    move = move.Before;
                }
                return count;
            }
        }
        public int AfterNum { get { return _afterMoves?.Count ?? 0; } }
        public Piece ToPiece { get; set; }

        public void Done(Board board)
        {
            var toSeat = board[CoordPair.ToCoord];
            ToPiece = toSeat.Piece;
            board[CoordPair.FromCoord].MoveTo(toSeat, Piece.NullPiece);
        }
        public void Undo(Board board)
            => board[CoordPair.ToCoord].MoveTo(board[CoordPair.FromCoord], ToPiece);

        public bool HasAfter { get { return _afterMoves != null; } }

        public Move AddAfterMove(CoordPair coordPair, string? remark = null, bool visible = true)
        {
            Move move = new(coordPair, remark, visible) { Before = this };
            (_afterMoves ??= new()).Add(move);
            return move;
        }

        // 前置着法列表，不含根节点、含自身this
        public List<Move> BeforeMoves()
        {
            List<Move> moves = new();
            Move move = this;
            while(move.Before != null)
            {
                moves.Insert(0, move);
                move = move.Before;
            }

            return moves;
        }
        // 后置着法列表
        public List<Move>? AfterMoves(VisibleType vtype = VisibleType.ALL)
        {
            if(_afterMoves == null || vtype == VisibleType.ALL)
                return _afterMoves;

            List<Move> moves = new(_afterMoves);
            if(vtype == VisibleType.TRUE)
                moves.RemoveAll(move => !move.Visible);
            else
                moves.RemoveAll(move => move.Visible);
            return moves;
        }
        // 同步变着列表
        public List<Move>? OtherMoves(VisibleType vtype = VisibleType.TRUE) => Before?.AfterMoves(vtype) ?? null;

        public void ClearAfterMovesError(ManualMove manualMove)
        {
            if(_afterMoves != null)
                _afterMoves.RemoveAll(move => !manualMove.CurMoveAccept(move.CoordPair));
        }

        new public string ToString() => String.Format($"{new string('\t', BeforeNum)}{Before?.Id}.{CoordPair.ToString()}_{Id} {Remark}\n");

        private List<Move>? _afterMoves;
    }
}
