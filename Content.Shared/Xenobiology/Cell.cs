using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[Serializable, NetSerializable]
public sealed class Cell
{
    [ViewVariables]
    public readonly ProtoId<CellPrototype> Id;

    public float Stability;
    public Color Color;
    public List<CellModifier> Modifiers;

    public Cell(CellPrototype cell)
    {
        Id = cell.ID;
        Color = cell.Color;
        Stability = cell.Stability;
        Modifiers = cell.Modifiers;
    }
}
