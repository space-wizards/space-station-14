using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[Serializable, NetSerializable]
public sealed class Cell
{
    [ViewVariables]
    public readonly ProtoId<CellPrototype> Id;

    public float Stability;
    public List<CellModifier> Modifiers;

    public Cell(CellPrototype cell)
    {
        Id = cell.ID;
        Stability = cell.Stability;
        Modifiers = cell.Modifiers;
    }
}
