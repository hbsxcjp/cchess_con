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

    internal class Move: IEnumerable
    {
        public Move(bool visible = true)
        {
            Before = null;
            ToPiece = Piece.NullPiece;

            Visible = visible;
            _AfterMoves = null;
        }
        public Move(CoordPair coordPair, string? remark = null, bool visible = true) : this(visible)
        {
            CoordPair = coordPair;
            Remark = remark;
        }

        static public Move CreateRootMove() { return new(); }

        public Move? Before { get; set; }
        public CoordPair CoordPair { get; set; }
        public string? Remark { get; set; }
        public bool Visible { get; set; }
        public int AfterNum { get { return _AfterMoves?.Count ?? 0; } }

        public Piece ToPiece { get; set; }
        public void Done(Board board)
        {
            var toSeat = board[CoordPair.ToCoord];
            ToPiece = toSeat.Piece;
            board[CoordPair.FromCoord].MoveTo(toSeat, Piece.NullPiece);
        }
        public void Undo(Board board)
            => board[CoordPair.ToCoord].MoveTo(board[CoordPair.FromCoord], ToPiece);

        public bool HasAfter { get { return _AfterMoves != null; } }

        public Move AddAfterMove(CoordPair coordPair, string? remark = null, bool visible = true)
            => AddAfterMove(new Move(coordPair, remark, visible));
        public Move AddOtherMove(CoordPair coordPair, string? remark = null, bool visible = true)
            => AddOtherMove(new Move(coordPair, remark, visible));

        // 前置着法堆栈，不含根节点、含自身this
        public Stack<Move> BeforeMoves()
        {
            Stack<Move> moves = new();
            Move move = this;
            while(move.Before != null)
            {
                moves.Push(move);
                move = move.Before;
            }

            return moves;
        }

        // 后置着法列表
        public List<Move>? AfterMoves(VisibleType vtype = VisibleType.ALL)
        {
            if(_AfterMoves == null || vtype == VisibleType.ALL)
                return _AfterMoves;

            List<Move> moves = new(_AfterMoves);
            if(vtype == VisibleType.TRUE)
                moves.RemoveAll(move => !move.Visible);
            else
                moves.RemoveAll(move => move.Visible);
            return moves;
        }
        // 同步变着列表
        public List<Move>? OtherMoves(VisibleType vtype = VisibleType.TRUE)
        {
            return Before?.AfterMoves(vtype) ?? null;
        }

        new public string ToString() => CoordPair.ToString() + Remark + '\n';

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();

        public MoveEnum GetEnumerator() => new(this);

        private Move AddAfterMove(Move move)
        {
            move.Before = this;
            (_AfterMoves ??= new()).Add(move);
            return move;
        }
        private Move AddOtherMove(Move move)
        {
            Before?.AddAfterMove(move);
            return move;
        }

        private List<Move>? _AfterMoves;
    }

    internal class MoveEnum: IEnumerator
    {
        public MoveEnum(Move topMove)
        {
            TopMove = topMove;
            _current = topMove;
            _queueMoves = new();

            Reset();
        }

        public void Reset()
        {
            _current = TopMove;
            _queueMoves.Clear();
            EnqueueAfterMoves(TopMove);
        }

        public bool MoveNext()
        {
            if(_queueMoves.Count > 0)
            {
                _current = _queueMoves.Dequeue();
                EnqueueAfterMoves(_current);
                return true;
            }

            return false;
        }

        object IEnumerator.Current { get { return Current; } }

        public Move TopMove { get; }
        public Move Current { get { return _current; } }

        private void EnqueueAfterMoves(Move beforeMove)
        {
            var afterMoves = beforeMove.AfterMoves();
            if(afterMoves == null)
                return;

            foreach(var move in afterMoves)
                _queueMoves.Enqueue(move);
        }

        private Move _current;
        private readonly Queue<Move> _queueMoves;
    }


}
