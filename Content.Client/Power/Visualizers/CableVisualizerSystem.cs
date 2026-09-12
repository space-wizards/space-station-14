using Content.Client.SubFloor;
using Content.Shared.Wires;
using Robust.Client.GameObjects;

namespace Content.Client.Power.Visualizers;

public sealed partial class CableVisualizerSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    [SubscribeLocalEvent(after: [typeof(SubFloorHideSystem)])]
    private void OnAppearanceChange(Entity<CableVisualizerComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.Visible)
        {
            // This entity is probably below a floor and is not even visible to the user -> don't bother updating sprite data.
            // Note that if the subfloor visuals change, then another AppearanceChangeEvent will get triggered.
            return;
        }

        if (!args.TryGetData<WireVisDirFlags>(WireVisVisuals.ConnectedMask, out var mask))
            mask = WireVisDirFlags.None;

        _sprite.LayerSetRsiState((ent, args.Sprite), 0, $"{ent.Comp.StatePrefix}{(int)mask}");
        if (ent.Comp.ExtraLayerPrefix != null)
            _sprite.LayerSetRsiState((ent, args.Sprite), 1, $"{ent.Comp.ExtraLayerPrefix}{(int)mask}");
    }
}
