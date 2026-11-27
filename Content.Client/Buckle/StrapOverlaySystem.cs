using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Robust.Client.GameObjects;

namespace Content.Client.Buckle;

/// <summary>
/// Renders extra visual layers while this strap is occupied.
/// </summary>
public sealed partial class StrapOverlaySystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<StrapOverlayComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<bool>(ent, StrapVisuals.State, out var occupied, args.Component) ||
            !occupied)
        {
            RemoveOverlay(ent);
            return;
        }

        EnsureOverlay(ent);
    }

    [SubscribeLocalEvent]
    private void OnStrapOverlayShutdown(Entity<StrapOverlayComponent> ent, ref ComponentShutdown args)
    {
        RemoveOverlay(ent);
    }

    private void EnsureOverlay(Entity<StrapOverlayComponent> ent)
    {
        if (ent.Comp.OverlayEntity is { } existing)
        {
            if (Exists(existing))
                return;

            ent.Comp.OverlayEntity = null;
        }

        var proxy = SpawnAttachedTo(ent.Comp.OverlayPrototype, ent.Owner.ToCoordinates());
        if (!TryComp<SpriteComponent>(proxy, out var proxySprite))
        {
            Del(proxy);
            return;
        }

        _sprite.SetDrawDepth((proxy, proxySprite), ent.Comp.OverlayDrawDepth);

        foreach (var data in ent.Comp.Layers)
        {
            _sprite.AddLayer((proxy, proxySprite), data, null);
        }

        ent.Comp.OverlayEntity = proxy;
    }

    private void RemoveOverlay(Entity<StrapOverlayComponent> ent)
    {
        if (ent.Comp.OverlayEntity is not { } proxy)
            return;

        ent.Comp.OverlayEntity = null;
        QueueDel(proxy);
    }
}
