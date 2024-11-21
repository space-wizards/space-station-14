using Content.Shared.Xenobiology.Components.Container;
using Content.Shared.Xenobiology.Systems;
using Content.Shared.Xenobiology.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client.Xenobiology;

public sealed class CellVisualsSystem : SharedCellVisualsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellContainerVisualsComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<CellContainerVisualsComponent> ent, ref AppearanceChangeEvent args)
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

        args.Sprite?.LayerSetColor(CellContainerVisuals.DishLayer, color.Value);
    }
}
