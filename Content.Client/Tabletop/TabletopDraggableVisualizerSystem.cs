using System.Numerics;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tabletop;

public sealed class TabletopDraggableVisualizerSystem : VisualizerSystem<TabletopDraggableComponent>
{
    private static readonly Vector2 DraggedScale = new(1.25f);
    private static readonly Vector2 NotDraggedScale = Vector2.Zero;

    private const int DraggedDrawDepth = (int)Shared.DrawDepth.DrawDepth.Items + 1;
    private const int NotDraggedDrawDepth = (int)Shared.DrawDepth.DrawDepth.Items;

    protected override void OnAppearanceChange(EntityUid uid,
        TabletopDraggableComponent comp,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null &&
            AppearanceSystem.TryGetData<bool>(uid,
                TabletopItemVisuals.BeingDragged,
                out var beingDragged,
                args.Component))
        {
            args.Sprite.Scale = beingDragged ? DraggedScale : NotDraggedScale;
            args.Sprite.DrawDepth = beingDragged ? DraggedDrawDepth : NotDraggedDrawDepth;
        }
    }
}
