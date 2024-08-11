using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Events;
using Content.Shared.Xenobiology.Visuals;

namespace Content.Shared.Xenobiology.Systems;

public abstract class SharedCellDishVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellDishVisualsComponent, CellAdded>(OnCellAdded);
        SubscribeLocalEvent<CellDishVisualsComponent, CellRemoved>(OnCellRemoved);
    }

    private void OnCellAdded(Entity<CellDishVisualsComponent> ent, ref CellAdded args)
    {
        UpdateAppearance(ent);
    }

    private void OnCellRemoved(Entity<CellDishVisualsComponent> ent, ref CellRemoved args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<CellDishVisualsComponent> ent)
    {
        if (!TryComp<CellContainerComponent>(ent, out var containerComponent))
            return;

        _appearance.SetData(ent, CellDishVisuals.DishVisibility, !containerComponent.Empty);
    }
}
