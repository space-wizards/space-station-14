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
    public readonly Cell? SelectedCell;
    public readonly IReadOnlySet<Cell> SavedCells;

    public CellSequencerUiState(Cell? selectedCell, IReadOnlySet<Cell> savedCells)
    {
        SelectedCell = selectedCell;
        SavedCells = savedCells;
    }
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiSyncMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CellSequencerUiScanMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CellSequencerUiCopyMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CellSequencerUiAddMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CellSequencerUiRemoveMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class CellSequencerUiPrintMessage : BoundUserInterfaceMessage
{

}
