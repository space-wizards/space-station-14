using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[Serializable, NetSerializable]
public sealed class Cell : IEquatable<Cell>
{
    [ViewVariables]
    public readonly ProtoId<CellPrototype> Id;

    [ViewVariables]
    public readonly Color Color;

    [ViewVariables]
    public readonly string Name;

    [ViewVariables]
    public readonly float Stability;

    [ViewVariables]
    public readonly int Cost;

    [ViewVariables]
    public readonly List<ProtoId<CellModifierPrototype>> Modifiers;

    public Cell(Cell cell)
    {
        Id = cell.Id;
        Name = Loc.GetString(cell.Name);
        Color = cell.Color;
        Stability = cell.Stability;
        Cost = cell.Cost;
        Modifiers = cell.Modifiers;
    }

    public Cell(CellPrototype cell)
    {
        Id = cell.ID;
        Name = Loc.GetString(cell.Name);
        Color = cell.Color;
        Stability = cell.Stability;
        Cost = cell.Cost;
        Modifiers = cell.Modifiers;
    }

    public override bool Equals(object? obj)
    {
        return obj is Cell other && Equals(other);
    }

    public bool Equals(Cell? other)
    {
        return other is not null &&
               other.Id == Id &&
               other.Color == Color &&
               other.Stability.Equals(Stability) &&
               other.Cost == Cost &&
               other.Modifiers.SequenceEqual(Modifiers) &&
               other.Name == Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Color, Stability, Cost, Modifiers, Name);
    }

    public static bool operator ==(Cell cellA, Cell cellB)
    {
        return cellA.Equals(cellB);
    }

    public static bool operator !=(Cell cellA, Cell cellB)
    {
        return !cellA.Equals(cellB);
    }
}
