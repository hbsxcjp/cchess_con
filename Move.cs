using cchess_con;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
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

        public int Id { get; set; }
        public int BeforeId { get; set; }
        public Move? Before { get; set; }
        public bool IsRoot { get { return Before == null; } }
        public CoordPair CoordPair { get; set; }
        public string? Remark { get; set; }
        public bool Visible { get; set; }
        public int AfterNum { get { return _AfterMoves?.Count ?? 0; } }

        public Piece ToPiece { get; set; }

        public bool HasAfter { get { return _AfterMoves != null; } }
        //public bool HasOther { get { return (OtherMoves()?.Count ?? 0) > 0; } }

        static public Move CreateRootMove() { return new(); }
        public Move AddAfterMove(Move move)
        {
            move.Before = this;
            (_AfterMoves ??= new()).Add(move);
            return move;
        }
        public Move AddAfterMove(CoordPair coordPair, string? remark = null, bool visible = true)
        {
            return AddAfterMove(new Move(coordPair, remark, visible));
        }

        public Move AddOtherMove(Move move)
        {
            Before?.AddAfterMove(move);
            return move;
        }
        public Move AddOtherMove(CoordPair coordPair, string? remark = null)
        {
            return AddOtherMove(new Move(coordPair, remark));
        }

        //public List<Move> BeforeMoves()
        //{
        //    List<Move> moves = new();
        //    Move move = this;
        //    while(move.Before != null)
        //    {
        //        moves.Insert(0, move);
        //        move = move.Before;
        //    }

        //    return moves; // 含自身this
        //}

        public List<Move>? AfterMoves() => _AfterMoves;
        //public List<Move>? OtherMoves()
        //{
        //    var moves = Before?.AfterMoves() ?? null;
        //    if(moves != null)
        //        moves.Remove(this);

        //    return (moves?.Count ?? 0) == 0 ? null : moves;
        //}
        new public string ToString()
        {
            return CoordPair.ToString() + Remark;
        }

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();

        public MoveEnum GetEnumerator() => new(this);

        private List<Move>? _AfterMoves;
    }

    internal class MoveEnum: IEnumerator
    {
        public MoveEnum(Move topMove)
        {
            TopMove = topMove;
            _queueMoves = new();

            Reset();
        }

        public void Reset()
        {
            _queueMoves.Clear();
            EnqueueAfterMoves(TopMove);
        }

        public bool MoveNext() { return _queueMoves.Count > 0; }

        object IEnumerator.Current { get { return Current; } }

        public Move TopMove { get; }
        public Move Current
        {
            get
            {
                try
                {
                    Move move = _queueMoves.Dequeue();
                    EnqueueAfterMoves(move);
                    return move;
                }
                catch(IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void EnqueueAfterMoves(Move beforeMove)
        {
            var afterMoves = beforeMove.AfterMoves();
            if(afterMoves == null)
                return;

            foreach(var move in afterMoves)
                _queueMoves.Enqueue(move);
        }

        private readonly Queue<Move> _queueMoves;
    }


}
