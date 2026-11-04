public readonly struct Coordinates
{
    public readonly int Row;
    public readonly int Column;

    public Coordinates(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public override bool Equals(object obj) => obj is Coordinates c && c.Row == Row && c.Column == Column;
    public override int GetHashCode() => System.HashCode.Combine(Row, Column);
    public override string ToString() => $"[{Row},{Column}]";
}
