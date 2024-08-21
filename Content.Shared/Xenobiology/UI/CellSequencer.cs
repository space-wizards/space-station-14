using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellSequencerUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiState : BoundUserInterfaceState
{
    public readonly IReadOnlyList<Cell> InsideCells;
    public readonly IReadOnlyList<Cell> RemoteCells;

    public CellSequencerUiState(IReadOnlyList<Cell> insideCell, IReadOnlyList<Cell> remoteCells)
    {
        InsideCells = insideCell;
        RemoteCells = remoteCells;
    }
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiSyncMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CellSequencerUiCopyMessage : BoundUserInterfaceMessage
{
    public readonly Cell? Cell;

    public CellSequencerUiCopyMessage(Cell? cell)
    {
        Cell = cell;
    }
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiAddMessage : BoundUserInterfaceMessage
{
    public readonly Cell? Cell;

    public CellSequencerUiAddMessage(Cell? cell)
    {
        Cell = cell;
    }
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiRemoveMessage : BoundUserInterfaceMessage
{
    public readonly Cell? Cell;
    public readonly bool Remote;

    public CellSequencerUiRemoveMessage(Cell? cell, bool remote)
    {
        Cell = cell;
        Remote = remote;
    }
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiPrintMessage : BoundUserInterfaceMessage
{
    public readonly Cell? Cell;

    public CellSequencerUiPrintMessage(Cell? cell)
    {
        Cell = cell;
    }
}
