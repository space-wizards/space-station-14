using Content.Shared.Xenoborgs.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoborgs.Systems;

public sealed class XenoborgVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoborgVisualsComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<XenoborgVisualsComponent, ComponentShutdown>(OnCompShutdown);
    }

    private void OnAfterHandleState(Entity<XenoborgVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (ent.Comp.FallbackSprite is null)
            return;

        var index = _sprite.LayerMapReserve((ent.Owner, sprite), ent.Comp.LayerMap);

        if (TryComp<XenoborgVisualsComponent>(ent, out var xenoborg))
        {
            _sprite.LayerSetSprite((ent.Owner, sprite), index, ent.Comp.FallbackSprite);
        }

        _sprite.LayerSetVisible((ent.Owner, sprite), index, true);
        sprite.LayerSetShader(index, "unshaded");
    }

    private void OnCompShutdown(Entity<XenoborgVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var index = _sprite.LayerMapGet((ent.Owner, sprite), ent.Comp.LayerMap);
        _sprite.LayerSetVisible((ent.Owner, sprite), index, false);
    }
}
