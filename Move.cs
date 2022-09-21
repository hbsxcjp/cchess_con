using cchess_con;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    internal struct MoveData
    {
        public MoveData(Move move)
        {
            CoordPair coordPair = move.CoordPair;
            Visible = move.Visible;
            FromCoordValue = (byte)(coordPair.FromCoord.row << 4 | coordPair.FromCoord.col);
            ToCoordValue = (byte)(coordPair.ToCoord.row << 4 | coordPair.ToCoord.col);
            //AfterNum = (byte)move.AfterNum;
            Remark = move.Remark;
        }

        public bool Visible;
        public byte FromCoordValue;
        public byte ToCoordValue;
        //public byte AfterNum;
        public string? Remark;
    }

    internal class Move: IEnumerable
    {
        protected Move()
        {
            Before = null;

            Visible = true;
            //CoordPair = new CoordPair();
            //Remark = null;

            ToPiece = Piece.NullPiece;
            _AfterMoves = null;
        }
        protected Move(CoordPair coordPair, string? remark = null) : this()
        {
            CoordPair = coordPair;
            Remark = remark;
        }
        protected Move(MoveData moveDate) : this()
        {
            Visible = moveDate.Visible;
            CoordPair = new CoordPair(
                new Coord(moveDate.FromCoordValue >> 4, moveDate.FromCoordValue & 0x0F),
                new Coord(moveDate.ToCoordValue >> 4, moveDate.ToCoordValue & 0x0F));
            Remark = moveDate.Remark;
        }

        public Move? Before { get; set; }
        public bool IsRoot { get { return Before == null; } }
        public bool Visible { get; set; }
        public CoordPair CoordPair { get; set; }
        public string? Remark { get; set; }
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
        public Move AddAfterMove(MoveData moveData)
        {
            return AddAfterMove(new Move(moveData));
        }
        public Move AddAfterMove(CoordPair coordPair, string? remark = null)
        {
            return AddAfterMove(new Move(coordPair, remark));
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

            _afterMoves = new();
            _queueMoves = new();

            Reset();
        }

        public bool MoveNext()
        {
            _position++;
            if(_position >= _afterMoves.Count)
                DequeueAfterMoves();

            return _position < _afterMoves.Count;
        }

        public void Reset()
        {
            _position = -1;
            _afterMoves.Clear();
            _queueMoves.Clear();

            _afterMoves.Add(TopMove);
            EnqueueAfterMoves(TopMove);
        }

        object IEnumerator.Current { get { return Current; } }

        public Move TopMove { get; }
        public Move Current
        {
            get
            {
                try
                {
                    return _afterMoves[_position];
                }
                catch(IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void EnqueueAfterMoves(Move move)
        {
            var afterMoves = move.AfterMoves();
            if(afterMoves != null)
                _queueMoves.Enqueue(afterMoves);
        }

        private void DequeueAfterMoves()
        {
            if(_queueMoves.Count == 0)
                return;

            _position = 0;
            _afterMoves = _queueMoves.Dequeue();
            foreach(var move in _afterMoves)
                EnqueueAfterMoves(move);
        }

        private int _position;
        private List<Move> _afterMoves;
        private readonly Queue<List<Move>> _queueMoves;
    }


}
