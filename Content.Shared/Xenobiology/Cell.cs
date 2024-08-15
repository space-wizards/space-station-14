using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[Serializable, NetSerializable]
public sealed class Cell
{
    [ViewVariables]
    public readonly ProtoId<CellPrototype> Id;

    [ViewVariables]
    public Color Color;

    [ViewVariables]
    public float Stability;

    [ViewVariables]
    public int Cost;

    [ViewVariables]
    public List<CellModifier> Modifiers;

    public Cell(CellPrototype cell)
    {
        Id = cell.ID;
        Color = cell.Color;
        Stability = cell.Stability;
        Cost = cell.Cost;
        Modifiers = cell.Modifiers;
    }
}
