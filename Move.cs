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
            AfterNum = (byte)move.AfterNum;
            Remark = move.Remark;
        }

        public bool Visible;
        public byte FromCoordValue;
        public byte ToCoordValue;
        public byte AfterNum;
        public string? Remark;
    }

    internal class Move
    {
        protected Move()
        {
            Before = null;

            Visible = false;
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
            if(Before != null)
                Before.AddAfterMove(move);

            return move;
        }
        public Move AddOtherMove(CoordPair coordPair, string? remark = null)
        {
            return AddOtherMove(new Move(coordPair, remark));
        }

        public Move? Before { get; set; }
        public bool Visible { get; set; }
        public CoordPair CoordPair { get; set; }
        public string? Remark { get; set; }
        public int AfterNum { get { return _AfterMoves?.Count ?? 0; } }
        public Piece ToPiece { get; set; }
        virtual public bool IsRoot { get { return false; } }

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
        new public string ToString()
        {
            return CoordPair.ToString() + Remark + '\n';
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

        override public bool IsRoot { get { return true; } }
        static new public bool Visible { get { return true; } }
        public bool EnumVisible { get; set; }

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();

        public MoveEnum GetEnumerator() => new(this);
    }

    internal class MoveEnum: IEnumerator
    {
        public MoveEnum(RootMove rootMove)
        {
            RootMove = rootMove;

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

            EnqueueAfterMoves(RootMove);
            DequeueAfterMoves();
        }

        object IEnumerator.Current { get { return Current; } }

        public RootMove RootMove { get; }
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
            var afterMoves = move.AfterMoves(RootMove.EnumVisible);
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

        private int _position;
        private List<Move> _curMoves;
        private readonly Queue<List<Move>> _queueMoves;
    }


}
