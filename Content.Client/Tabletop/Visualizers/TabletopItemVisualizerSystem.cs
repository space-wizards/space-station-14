using Content.Shared.Tabletop;
using Robust.Client.GameObjects;

namespace Content.Client.Tabletop.Visualizers;

public sealed class TabletopItemVisualizerSystem : VisualizerSystem<TabletopItemVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, TabletopItemVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // TODO: maybe this can work more nicely, by maybe only having to set the item to "being dragged", and have
        //  the appearance handle the rest
        if (AppearanceSystem.TryGetData<Vector2>(uid, TabletopItemVisuals.Scale, out var scale, args.Component))
        {
            args.Sprite.Scale = scale;
        }

        if (AppearanceSystem.TryGetData<int>(uid, TabletopItemVisuals.DrawDepth, out var drawDepth, args.Component))
        {
            args.Sprite.DrawDepth = drawDepth;
        }
    }
}
