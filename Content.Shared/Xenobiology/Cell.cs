using System.Linq;
using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Components.Container;
using Content.Shared.Xenobiology.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

/// <summary>
/// Displays the current information about the cell,
/// it may differ from the prototype due to changes and other mutations,
/// so to work with cells we allocate a separate class,
/// which is also passed between the client and the server.
/// </summary>
/// <remarks>
/// Cells are just as immutable,
/// if you need a new cell you have to create a new one,
/// example:
/// <code>
/// var cellCopy = new Cell(cellOther);
/// </code>
/// </remarks>
/// <seealso cref="CellPrototype"/>
/// <seealso cref="CellContainerComponent"/>
/// <seealso cref="SharedCellSystem"/>
[Serializable, NetSerializable]
public sealed class Cell : IEquatable<Cell>
{
    /// <summary>
    /// Reflects the prototype on which the cell is based,
    /// if it is generated, this value will be null.
    /// </summary>
    [ViewVariables]
    public readonly ProtoId<CellPrototype>? Id;

    /// <summary>
    /// The color of a cell
    /// affecting only its display in the world or consoles.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetMergedColor"/>
    [ViewVariables]
    public readonly Color Color;

    /// <summary>
    /// Cell name, this can be changed by the player,
    /// don't use this to index cells.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetMergedName"/>
    [ViewVariables]
    public readonly string Name;

    /// <summary>
    /// The current stability of the cell,
    /// this affects the chance of successful splice and many other factors.
    /// </summary>
    ///
    /// <seealso cref="SharedCellSystem.GetMergedStability"/>
    [ViewVariables]
    public readonly float Stability;

    /// <summary>
    /// Cost in materials of the device for printing as well as splicing of the cell.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetMergedCost"/>
    [ViewVariables]
    public readonly int Cost;

    /// <summary>
    /// A list of modifier prototypes,
    /// each of which is applied to a cell when it is introduced into the <see cref="CellContainerComponent"/>,
    /// if the <see cref="CellContainerComponent.AllowModifiers"/> field is true.
    /// </summary>
    [ViewVariables]
    public readonly List<ProtoId<CellModifierPrototype>> Modifiers;

    public Cell(ProtoId<CellPrototype>? id,
        Color color,
        string name,
        float stability,
        int cost,
        List<ProtoId<CellModifierPrototype>> modifiers)
    {
        Id = id;
        Color = color;
        Name = name;
        Stability = stability;
        Cost = cost;
        Modifiers = modifiers;
    }

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
