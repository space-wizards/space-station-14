using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Events;
using Content.Shared.Xenobiology.Visuals;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology.Systems;

[PublicAPI]
public abstract class SharedCellSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellGenerationComponent, ComponentInit>(OnCellGenerationInit);
    }

    private void OnCellGenerationInit(Entity<CellGenerationComponent> ent, ref ComponentInit args)
    {
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

    public void AddCell(Entity<CellContainerComponent?> ent, ProtoId<CellPrototype> cellId)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        AddCell(ent, new Cell(_prototype.Index(cellId)));
    }

    public void AddCell(Entity<CellContainerComponent?> ent, Cell cell)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Cells.Add(cell);
        Dirty(ent);

        var ev = new CellAdded(GetNetEntity(ent), cell);
        RaiseLocalEvent(ent, ev);

        if (!ent.Comp.AllowModifiers)
            return;

        foreach (var modifierId in cell.Modifiers)
        {
            if (!_prototype.TryIndex(modifierId, out var modifierProto))
                continue;

            foreach (var modifier in modifierProto.Modifiers)
            {
                modifier.OnAdd(ent!, cell, EntityManager);
            }
        }
    }

    public void RemoveCell(Entity<CellContainerComponent?> ent, Cell cell)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.Cells.Remove(cell))
            return;

        Dirty(ent);

        var ev = new CellRemoved(GetNetEntity(ent), cell);
        RaiseLocalEvent(ent, ev);

        if (!ent.Comp.AllowModifiers)
            return;

        foreach (var modifierId in cell.Modifiers)
        {
            if (!_prototype.TryIndex(modifierId, out var modifierProto))
                continue;

            foreach (var modifier in modifierProto.Modifiers)
            {
                modifier.OnRemove(ent!, cell, EntityManager);
            }
        }
    }

    public void ClearCells(Entity<CellContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // I'm lazy fix later
        foreach (var cell in new List<Cell>(ent.Comp.Cells))
        {
            RemoveCell(ent, cell);
        }
    }

    /// <summary>
    /// Adds all cells from the target container to the current container.
    /// </summary>
    public void CollectCells(Entity<CellContainerComponent?> ent, Entity<CellContainerComponent?> target)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(target, ref target.Comp))
            return;

        foreach (var cell in target.Comp.Cells)
        {
            AddCell(ent, cell);
        }
    }

    protected void UpdateCollectorAppearance(Entity<CellCollectorComponent, CellContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        _appearance.SetData(ent, CellCollectorVisuals.State, ent.Comp2.Empty);
    }

    public static float GetMergedStability(Cell cellA, Cell cellB)
    {
        var max = Math.Max(cellA.Stability, cellB.Stability);
        var delta = Math.Abs(cellA.Stability - cellB.Stability);

        // This is a simple but not the best implementation,
        // I think more thought should be given to this formula
        return max * (1 - delta);
    }

    public static string GetMergedName(Cell cellA, Cell cellB)
    {
        var nameA = cellA.Name[..(cellA.Name.Length / 2)];
        var nameB = cellB.Name[(cellA.Name.Length / 2)..];
        return $"{nameA}{nameB}";
    }

    public static Color GetMergedColor(Cell cellA, Cell cellB)
    {
        return Color.InterpolateBetween(cellA.Color, cellB.Color, 0.5f);
    }

    public static int GetMergedCost(Cell cellA, Cell cellB)
    {
        return cellA.Cost + cellB.Cost;
    }
}
