namespace Content.Shared.Xenobiology.Events;

public sealed class CellRemoved : EntityEventArgs
{
    public readonly NetEntity Entity;
    public readonly Cell Cell;

    public CellRemoved(NetEntity entity, Cell cell)
    {
        Entity = entity;
        Cell = cell;
    }
}
