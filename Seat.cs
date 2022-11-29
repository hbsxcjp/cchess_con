namespace CChess;

internal class Seat
{
    public Seat(Coord coord)
    {
        Coord = coord;
        _piece = Piece.NullPiece;
    }

    public Coord Coord { get; }
    public Piece Piece
    {
        get { return _piece; }
        set
        {
            _piece.Seat = Null;

            value.Seat = this;
            _piece = value;
        }
    }
    public bool IsNull { get { return this == Null; } }
    public bool HasNullPiece { get { return Piece == Piece.NullPiece; } }

    public void MoveTo(Seat toSeat, Piece fromPiece)
    {
        Piece piece = Piece;
        Piece = fromPiece;
        toSeat.Piece = piece;
    }

    public static Seat[,] CreatSeats()
    {
        var seats = new Seat[Coord.RowCount, Coord.ColCount];
        foreach(var (row, col) in Coord.GetAllRowCol())
            seats[row, col] = new(new(row, col));

        return seats;
    }
    public override string ToString() => string.Format($"{Coord}:{_piece}");

    public static readonly Seat Null = new(Coord.Null);

    private Piece _piece;
}


