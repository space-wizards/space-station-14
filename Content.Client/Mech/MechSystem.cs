using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Mech;

/// <inheritdoc/>
public sealed partial class MechSystem : SharedMechSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(Entity<MechComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_sprite.LayerExists((ent, args.Sprite), MechVisualLayers.Base))
            return;

        var state = ent.Comp.BaseState;
        var drawDepth = DrawDepth.Mobs;
        if (ent.Comp.BrokenState != null && args.TryGetData<bool>(uid, MechVisuals.Broken, out var broken) && broken)
        {
            state = ent.Comp.BrokenState;
            drawDepth = DrawDepth.SmallMobs;
        }
        else if (ent.Comp.OpenState != null && args.TryGetData<bool>(MechVisuals.Open, out var open) && open)
        {
            state = ent.Comp.OpenState;
            drawDepth = DrawDepth.SmallMobs;
        }

        _sprite.LayerSetRsiState((ent, args.Sprite), MechVisualLayers.Base, state);
        _sprite.SetDrawDepth((ent, args.Sprite), (int)drawDepth);
    }
}
