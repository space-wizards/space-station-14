using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology;

public abstract partial class SharedCellSystem : EntitySystem
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

        var ev = new CellAdded(GetNetEntity(ent), cell);
        RaiseLocalEvent(ent, ev);

        ent.Comp.Cells.Add(cell);

        foreach (var modifier in cell.Modifiers)
        {
            modifier.OnAdd(ent!, cell, EntityManager);
        }

        Dirty(ent);
    }

    public void RemoveCell(Entity<CellContainerComponent?> ent, Cell cell)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.Cells.Remove(cell))
            return;

        var ev = new CellRemoved(GetNetEntity(ent), cell);
        RaiseLocalEvent(ent, ev);

        foreach (var modifier in cell.Modifiers)
        {
            modifier.OnRemove(ent!, cell, EntityManager);
        }

        Dirty(ent);
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
}
