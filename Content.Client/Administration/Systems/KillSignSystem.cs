using System.Numerics;
using Content.Shared.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Administration.Systems;

public sealed class KillSignSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KillSignComponent, ComponentStartup>(KillSignAdded);
        SubscribeLocalEvent<KillSignComponent, ComponentShutdown>(KillSignRemoved);
    }

    private void KillSignRemoved(EntityUid uid, KillSignComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), KillSignKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((uid, sprite), layer);
    }

    private void KillSignAdded(EntityUid uid, KillSignComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), KillSignKey.Key, out var _, false))
            return;

        var adj = _sprite.GetLocalBounds((uid, sprite)).Height / 2 + ((1.0f / 32) * 6.0f);

        var layer = _sprite.AddLayer((uid, sprite), new SpriteSpecifier.Rsi(new ResPath("Objects/Misc/killsign.rsi"), "sign"));
        _sprite.LayerMapSet((uid, sprite), KillSignKey.Key, layer);

        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");
    }

    private enum KillSignKey
    {
        Key,
    }
}
