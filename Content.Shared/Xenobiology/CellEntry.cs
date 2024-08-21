namespace Content.Shared.Xenobiology;

public readonly struct CellEntry
{
    public readonly int Id;
    public readonly Cell Cell;

    public CellEntry(int id, Cell cell)
    {
        Id = id;
        Cell = cell;
    }
}
