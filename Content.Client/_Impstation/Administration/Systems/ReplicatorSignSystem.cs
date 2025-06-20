using System.Numerics;
using Content.Shared._Impstation.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Impstation.Administration.Components;

public sealed class ReplicatorSignSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ReplicatorSignComponent, ComponentStartup>(ReplicatorSignAdded);
        SubscribeLocalEvent<ReplicatorSignComponent, ComponentShutdown>(ReplicatorSignRemoved);
    }

    private void ReplicatorSignRemoved(Entity<ReplicatorSignComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), ReplicatorSignKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((ent, sprite), layer);
    }

    private void ReplicatorSignAdded(Entity<ReplicatorSignComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((ent, sprite), ReplicatorSignKey.Key, out var _, false))
            return;

        var layer = _sprite.AddLayer((ent, sprite), new SpriteSpecifier.Rsi(ent.Comp.SpritePath, "sign"));
        _sprite.LayerMapSet((ent, sprite), ReplicatorSignKey.Key, layer);

        sprite.LayerSetShader(layer, "unshaded");
    }

    private enum ReplicatorSignKey
    {
        Key,
    }
}
