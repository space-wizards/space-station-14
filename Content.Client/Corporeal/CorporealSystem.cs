using Content.Shared.Corporeal.Components;
using Content.Shared.Corporeal.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;

namespace Content.Client.Corporeal;

public sealed partial class CorporealSystem : SharedCorporealSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private const string OverlayLayerKey = "corporeal-overlay";

    protected override void OnApplied(Entity<CorporealStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!TryComp<SpriteComponent>(args.Target, out var sprite))
            return;

        var target = (args.Target, sprite);

        if (_sprite.LayerMapTryGet(target, OverlayLayerKey, out _, false))
            return;

        var layer = _sprite.AddLayer(target, ent.Comp.Sprite);
        _sprite.LayerMapSet(target, OverlayLayerKey, layer);

        base.OnApplied(ent, ref args);
    }

    protected override void OnRemoved(Entity<CorporealStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!TryComp<SpriteComponent>(args.Target, out var sprite))
            return;

        _sprite.RemoveLayer((args.Target, sprite), OverlayLayerKey, false);

        base.OnRemoved(ent, ref args);
    }
}
