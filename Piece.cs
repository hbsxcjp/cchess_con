using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    enum PieceColor
    {
        RED,
        BLACK
    }

    enum PieceKind
    {
        KING,
        ADVISOR,    
        BISHOP,    
        KNIGHT,    
        ROOK,    
        CANNON,
        PAWN
    }

    abstract internal class Piece
    {
        public Piece(PieceColor color)
        {
            Color = color;
        }

        public PieceColor Color{ get; set; }

        abstract public PieceKind Kind { get; }

        public Seat? Seat { get; set; }

        abstract public char Char { get; }

        abstract public char Name { get; }

        virtual public char PrintName() { return Name; }

        public string ShowString { get { return (Color == PieceColor.RED ? "红" : "黑") + PrintName() + Char; } }

    }

    internal class King: Piece
    {
        public King(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.KING; } }

        override public char Char { get { return Color == PieceColor.RED ? 'K' : 'k'; } }

        override public char Name { get { return Color == PieceColor.RED ? '帅' : '将'; } }
    }

    internal class Advisor: Piece
    {
        public Advisor(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.ADVISOR; } }

        override public char Char { get { return Color == PieceColor.RED ? 'A' : 'a'; } }

        override public char Name { get { return Color == PieceColor.RED ? '仕' : '士'; } }
    }

    internal class Bishop: Piece
    {
        public Bishop(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.BISHOP; } }

        override public char Char { get { return Color == PieceColor.RED ? 'B' : 'b'; } }

        override public char Name { get { return Color == PieceColor.RED ? '相' : '象'; } }
    }

    internal class Knight: Piece
    {
        public Knight(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.KNIGHT; } }

        override public char Char { get { return Color == PieceColor.RED ? 'N' : 'n'; } }

        override public char Name { get { return '马'; } }

        override public char PrintName() { return Color == PieceColor.RED ? Name : '馬'; }
    }

    internal class Rook: Piece
    {
        public Rook(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.ROOK; } }

        override public char Char { get { return Color == PieceColor.RED ? 'R' : 'r'; } }

        override public char Name { get { return '车'; } }

        override public char PrintName() { return Color == PieceColor.RED ? Name : '車'; }
    }

    internal class Cannon: Piece
    {
        public Cannon(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.CANNON; } }

        override public char Char { get { return Color == PieceColor.RED ? 'C' : 'c'; } }

        override public char Name { get { return '炮'; } }

        override public char PrintName() { return Color == PieceColor.RED ? Name : '砲'; }
    }

    internal class Pawn: Piece
    {
        public Pawn(PieceColor color) : base(color) { }

        override public PieceKind Kind { get { return PieceKind.PAWN; } }

        override public char Char { get { return Color == PieceColor.RED ? 'P' : 'p'; } }

        override public char Name { get { return Color == PieceColor.RED ? '兵' : '卒'; } }
    }

}
