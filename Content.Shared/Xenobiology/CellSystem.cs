using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology;

public sealed class CellSystem : EntitySystem
{
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

        var cell = new Cell(_prototype.Index(cellId));

        var ev = new CellAdded(GetNetEntity(ent), cell);
        RaiseLocalEvent(ent, ev, true);

        ent.Comp.Cells.Add(cell);

        foreach (var modifier in cell.Modifiers)
        {
            modifier.Modify(ent!, cell, EntityManager);
        }

        Dirty(ent);
    }
}
