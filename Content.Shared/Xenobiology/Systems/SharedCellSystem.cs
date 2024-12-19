using Content.Shared.Xenobiology.Components.Container;
using Content.Shared.Xenobiology.Events;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology.Systems;

/// <summary>
/// The system responsible for the operation, copy, translate, update and splicing of <see cref="Cell"/>
/// and the containers in which they are contained,
/// without visual effects, only direct interaction.
/// </summary>
/// <seealso cref="CellContainerComponent"/>
/// <seealso cref="CellGenerationComponent"/>
/// <seealso cref="SharedCellVisualsSystem"/>
[PublicAPI]
public abstract partial class SharedCellSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellGenerationComponent, ComponentInit>(OnCellGenerationInit);
    }

    private void OnCellGenerationInit(Entity<CellGenerationComponent> ent, ref ComponentInit args)
    {
        // It doesn't make sense to add cells to a container,
        // if there is no container component,
        // it's better to warn the developer about it
        if (!TryComp<CellContainerComponent>(ent, out var container))
        {
            Log.Error($"Can't ensure cells to {ent} without {nameof(CellContainerComponent)}!");
            return;
        }

        foreach (var cellId in ent.Comp.Cells)
        {
            AddCell((ent, container), cellId);
        }
    }

    /// <summary>
    /// Adds a new <see cref="Cell"/> instance created from the prototype to the container.
    /// </summary>
    /// <seealso cref="CellPrototype"/>
    public bool AddCell(Entity<CellContainerComponent?> ent, ProtoId<CellPrototype> cellId)
    {
        // Check the availability of the component and the prototype we need
        if (!Resolve(ent, ref ent.Comp) || !_prototype.TryIndex(cellId, out var cellPrototype))
            return false;

        return AddCell(ent, new Cell(cellPrototype));
    }

    /// <summary>
    /// Adds a new <see cref="Cell"/> instance to the container.
    /// </summary>
    /// <seealso cref="ApplyAddCellModifiers"/>
    public bool AddCell(Entity<CellContainerComponent?> ent, Cell cell)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // Add cell to container
        ent.Comp.Cells.Add(cell);
        Dirty(ent);

        // Create an event, and notify all subscribers about the addition of a cell
        var ev = new CellAdded(GetNetEntity(ent), cell);
        RaiseLocalEvent(ent, ev);

        if (ent.Comp.AllowModifiers)
        {
            ApplyAddCellModifiers(ent!, cell);
        }

        return true;
    }

    /// <summary>
    /// Remove a <see cref="Cell"/> instance from the container.
    /// </summary>
    /// <seealso cref="ApplyRemoveCellModifiers"/>
    public bool RemoveCell(Entity<CellContainerComponent?> ent, Cell cell)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // We try to remove the cell, if we failed,
        // then it is already removed, and we do not need further methods
        if (!ent.Comp.Cells.Remove(cell))
            return false;

        // Yes, this system uses predict,
        // but we still need to synchronize the state to avoid unnecessary problems
        Dirty(ent);

        // Create an event, and notify all subscribers about the removing of a cell
        var ev = new CellRemoved(GetNetEntity(ent), cell);
        RaiseLocalEvent(ent, ev);

        if (ent.Comp.AllowModifiers)
        {
            ApplyRemoveCellModifiers(ent!, cell);
        }

        return true;
    }

    /// <summary>
    /// Remove all <see cref="Cell"/> instances from the container.
    /// </summary>
    /// <seealso cref="RemoveCell"/>
    public void ClearCells(Entity<CellContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // while loop is used to avoid problems with collection iterating
        // and deleting its elements in same loop
        while (ent.Comp.Cells.Count != 0)
        {
            RemoveCell(ent, ent.Comp.Cells[0]);
        }
    }

    /// <summary>
    /// Adds all cells from the target container to the current container.
    /// </summary>
    /// <seealso cref="AddCell(Entity{CellContainerComponent?}, Cell)"/>
    public void CopyCells(Entity<CellContainerComponent?> ent, Entity<CellContainerComponent?> target)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(target, ref target.Comp))
            return;

        foreach (var cell in target.Comp.Cells)
        {
            AddCell(ent, cell);
        }
    }

    /// <summary>
    /// Adds all cells from the target container to the current container and clear it.
    /// </summary>
    /// <seealso cref="CopyCells"/>
    /// <seealso cref="ClearCells"/>
    public void TransferCells(Entity<CellContainerComponent?> ent, Entity<CellContainerComponent?> target)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(target, ref target.Comp))
            return;

        CopyCells(ent, target);
        ClearCells(ent);
    }
}
