using System.Numerics;
using Content.Shared.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Administration.Systems;

public sealed class KillSignSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

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
        // After receiving a new state for the component, we remove the old killsign and build a new one.
        // This is so changes to the sprite can be displayed live and allowing them to be edited via ViewVariables.
        // This could just update an existing sprite, but this is both easier and runs rarely anyway.
        RemoveKillsign(ent);
        AddKillsign(ent);
    }

    private void AddKillsign(Entity<KillSignComponent> ent)
    {
        // If we hide from owner and we ARE the owner, don't add a killsign.
        // This could use session specific networking to FULLY hide it, but I am too lazy right now.
        if (ent.Comp.HideFromOwner && _player.LocalEntity == ent)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((ent, sprite), KillSignKey.Key, out var _, false))
            return;

        if (ent.Comp.Sprite == null)
            return;

        var adj = _sprite.GetLocalBounds((ent, sprite)).Height / 2 + ((1.0f / 32) * 6.0f);

        var layer = _sprite.AddLayer((ent, sprite), ent.Comp.Sprite);
        _sprite.LayerMapSet((ent, sprite), KillSignKey.Key, layer);
        _sprite.LayerSetScale((ent, sprite), layer, ent.Comp.Scale);
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
