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
    public readonly int Material;
    public readonly bool HasContainer;

    public CellSequencerUiState(IReadOnlyList<Cell> insideCell, IReadOnlyList<Cell> remoteCells, int material, bool hasContainer)
    {
        InsideCells = insideCell;
        RemoteCells = remoteCells;
        Material = material;
        HasContainer = hasContainer;
    }
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiSyncMessage : BoundUserInterfaceMessage;

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
public sealed class CellSequencerUiReplaceMessage : BoundUserInterfaceMessage
{
    public readonly Cell? Cell;

    public CellSequencerUiReplaceMessage(Cell? cell)
    {
        Cell = cell;
    }
}
