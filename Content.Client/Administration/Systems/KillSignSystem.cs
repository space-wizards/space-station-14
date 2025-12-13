using System.Numerics;
using Content.Shared.Administration.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Administration.Systems;

public sealed class KillSignSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KillSignComponent, ComponentStartup>(KillSignAdded);
        SubscribeLocalEvent<KillSignComponent, ComponentShutdown>(KillSignRemoved);
        SubscribeLocalEvent<KillSignComponent, AfterAutoHandleStateEvent>(AfterAutoHandleState);
    }

    private void KillSignRemoved(Entity<KillSignComponent> ent, ref ComponentShutdown args)
    {
        RemoveKillsign(ent);
    }

    private void KillSignAdded(Entity<KillSignComponent> ent, ref ComponentStartup args)
    {
        AddKillsign(ent);
    }

    private void AfterAutoHandleState(Entity<KillSignComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RemoveKillsign(ent);
        AddKillsign(ent);
    }

    private void AddKillsign(Entity<KillSignComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((ent, sprite), KillSignKey.Key, out var _, false))
            return;

        if (ent.Comp.Sprite == null)
            return;

        var adj = _sprite.GetLocalBounds((ent, sprite)).Height / 2 + ((1.0f / 32) * 6.0f);

        var layer = _sprite.AddLayer((ent, sprite), ent.Comp.Sprite);
        _sprite.LayerMapSet((ent, sprite), KillSignKey.Key, layer);

        _sprite.LayerSetOffset((ent, sprite), layer, ent.Comp.DoOffset ? new Vector2(0.0f, adj) : new Vector2(0.0f, 0.0f));

        if (ent.Comp.ForceUnshaded)
            sprite.LayerSetShader(layer, "unshaded");
    }

    private void RemoveKillsign(Entity<KillSignComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), KillSignKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((ent, sprite), layer);
    }

    private enum KillSignKey
    {
        Key,
    }
}
