using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellularFusionUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiSyncMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CellularFusionUiState : BoundUserInterfaceState
{
    public readonly IReadOnlyList<Cell> RemoteCells;
    public readonly int Material;

    public CellularFusionUiState(IReadOnlyList<Cell> remoteCells, int material)
    {
        RemoteCells = remoteCells;
        Material = material;
    }
}
