using cchess_con;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal class Move
    {
        protected Move()
        {
            Before = null;
            CoordPair = null;
            Remark = null;
            ToPiece = Piece.NullPiece;

            Visible = false;
            _AfterMoves = null;
        }

        public Move AddAfterMove(CoordPair coordPair, string? remark = null)
        {
            Move move = new()
            {
                Before = this,
                CoordPair = coordPair,
                Remark = remark
            };

            (_AfterMoves ??= new()).Add(move);
            return move;
        }
        public Move AddOtherMove(CoordPair coordPair, string? remark = null)
        {
            if(Before == null)
                throw new Exception("开始节点不能添加兄弟着法！");

            return Before.AddAfterMove(coordPair, remark);
        }

        public Move? Before { get; set; }
        public CoordPair? CoordPair { get; set; }
        public string? Remark { get; set; }
        public Piece ToPiece { get; set; }
        public bool Visible { get; set; }
        static public bool IsRoot { get { return false; } }

        public bool HasAfter(bool enumVisible = true) => AfterMoves(enumVisible) != null;
        public bool HasOther(bool enumVisible = true) => (OtherMoves(enumVisible)?.Count ?? 0) > 0;

        public List<Move> BeforeMoves()
        {
            List<Move> moves = new();
            Move move = this;
            while(move.Before != null)
            {
                moves.Insert(0, move);
                move = move.Before;
            }

            return moves; // 含自身this
        }
        public List<Move>? AfterMoves(bool enumVisible) => GetMoves(_AfterMoves, enumVisible);
        public List<Move>? OtherMoves(bool enumVisible)
        {
            var moves = Before?.AfterMoves(enumVisible) ?? null;
            if(moves != null)
                moves.Remove(this);

            return (moves?.Count ?? 0) == 0 ? null : moves;
        }

        static private List<Move>? GetMoves(List<Move>? moves, bool enumVisible)
        {
            if(!enumVisible || moves == null)
                return moves;

            List<Move>? result = null;
            foreach(var move in moves)
                if(move.Visible)
                    (result ??= new()).Add(move);

            return result;
        }

        private List<Move>? _AfterMoves;
    }

    internal class RootMove: Move, IEnumerable
    {
        public RootMove() : base()
        {
            EnumVisible = true;
        }

        static new public bool IsRoot { get { return true; } }
        static new public bool Visible { get { return true; } }
        public bool EnumVisible { get; set; }

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();

        public MoveEnum GetEnumerator() => new(this);
    }

    internal class MoveEnum: IEnumerator
    {
        public MoveEnum(RootMove rootMove)
        {
            _rootMove = rootMove;

            _curMoves = new();
            _queueMoves = new();

            Reset();
        }

        public bool MoveNext()
        {
            _position++;
            return _position < _curMoves.Count || DequeueAfterMoves();
        }

        public void Reset()
        {
            _position = -1;
            _curMoves.Clear();
            _queueMoves.Clear();

            EnqueueAfterMoves(_rootMove);
        }

        object IEnumerator.Current { get { return Current; } }

        public Move Current
        {
            get
            {
                try
                {
                    return _curMoves[_position];
                }
                catch(IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void EnqueueAfterMoves(Move move)
        {
            var afterMoves = move.AfterMoves(_rootMove.EnumVisible);
            if(afterMoves != null)
                _queueMoves.Enqueue(afterMoves);
        }

        private bool DequeueAfterMoves()
        {
            if(_queueMoves.Count == 0)
                return false;

            _position = 0;
            _curMoves = _queueMoves.Dequeue();
            foreach(var move in _curMoves)
                EnqueueAfterMoves(move);

            return true;
        }

        private readonly RootMove _rootMove;

        private int _position;
        private List<Move> _curMoves;
        private readonly Queue<List<Move>> _queueMoves;
    }


}
