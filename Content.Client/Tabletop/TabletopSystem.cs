using System.Numerics;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopSystem : SharedTabletopSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    #region Event handlers
    /// <summary>
    /// Hologram handler: sets up the hologram to mimic another entity's sprite.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnHologramAppearanceChange(Entity<TabletopHologramComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // TODO: maybe this can work more nicely, by maybe only having to set the item to "being dragged", and have
        //  the appearance handle the rest
        if (!Appearance.TryGetData<string>(ent, TabletopItemVisuals.Prototype, out var protoId, args.Component)
            || ent.Comp.LastPrototype == protoId)
            return;

        ent.Comp.LastPrototype = protoId;

        if (ProtoMan.TryIndex(protoId, out var proto)
            && proto.TryComp<SpriteComponent>(out var protoSprite, Factory))
        {
            // HACK: we don't actually have an entity to pass, but the first parameter here is unused.
            var outSprite = CopyComp(EntityUid.Invalid, ent, protoSprite);
            outSprite.NoRotation = true;
        }

        // Reset our scale/draw depth after copying our new sprite, if the data exists.
        if (Appearance.TryGetData<Vector2>(ent, TabletopItemVisuals.Scale, out var scale, args.Component))
            _sprite.SetScale((ent, args.Sprite), scale);

        if (Appearance.TryGetData<int>(ent, TabletopItemVisuals.DrawDepth, out var drawDepth, args.Component))
            _sprite.SetDrawDepth((ent, args.Sprite), drawDepth);
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<TabletopDraggableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // TODO: maybe this can work more nicely, by maybe only having to set the item to "being dragged", and have
        //  the appearance handle the rest
        if (Appearance.TryGetData<Vector2>(ent, TabletopItemVisuals.Scale, out var scale, args.Component))
            _sprite.SetScale((ent, args.Sprite), scale);

        if (Appearance.TryGetData<int>(ent, TabletopItemVisuals.DrawDepth, out var drawDepth, args.Component))
            _sprite.SetDrawDepth((ent, args.Sprite), drawDepth);
    }
    #endregion
}
