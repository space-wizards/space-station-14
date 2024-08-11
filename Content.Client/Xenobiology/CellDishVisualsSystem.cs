using Content.Shared.Xenobiology.Components;
using Content.Shared.Xenobiology.Systems;
using Content.Shared.Xenobiology.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client.Xenobiology;

public sealed class CellDishVisualsSystem : SharedCellDishVisualsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellDishVisualsComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<CellDishVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<CellContainerComponent>(ent, out var containerComponent) || containerComponent.Empty)
            return;

        Color? color = null;
        foreach (var cell in containerComponent.Cells)
        {
            color ??= cell.Color;
            color = Color.InterpolateBetween(color.Value, cell.Color, 0.5f);
        }

        if (color is null)
            return;

        args.Sprite?.LayerSetColor(CellDishVisuals.DishLayer, color.Value);
    }
}
