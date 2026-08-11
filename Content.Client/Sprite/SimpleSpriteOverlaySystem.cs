using Content.Shared.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client.Sprite;

public sealed partial class SimpleSpriteOverlaySystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<SimpleSpriteOverlayComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var sprite = Comp<SpriteComponent>(ent);

        var index = _sprite.LayerMapReserve((ent.Owner, sprite), ent.Comp.LayerMap);

        _sprite.LayerSetSprite((ent.Owner, sprite), index, ent.Comp.OverlaySprite);
        _sprite.LayerSetVisible((ent.Owner, sprite), index, true);

        if (ent.Comp.Shader is not null)
            sprite.LayerSetShader(index, ent.Comp.Shader);
    }

    [SubscribeLocalEvent]
    private void OnCompShutdown(Entity<SimpleSpriteOverlayComponent> ent, ref ComponentShutdown args)
    {
        var sprite = Comp<SpriteComponent>(ent);

        if (_sprite.LayerMapTryGet((ent.Owner, sprite), ent.Comp.LayerMap, out var index, true))
            _sprite.LayerSetVisible((ent.Owner, sprite), index, false);
    }
}
