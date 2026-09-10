using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Buckle;

/// <summary>
/// Renders extra visual layers over entities buckled to this strap.
/// </summary>
public sealed partial class StrapOverlaySystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly EntProtoId OverlayPrototype = "StrapVisualOverlay";

    [SubscribeLocalEvent]
    private void OnStrapped(Entity<StrapOverlayComponent> ent, ref StrappedEvent args) => EnsureOverlay(ent);

    [SubscribeLocalEvent]
    private void OnUnstrapped(Entity<StrapOverlayComponent> ent, ref UnstrappedEvent args) => EnsureOverlay(ent);

    [SubscribeLocalEvent]
    private void OnStrapState(Entity<StrapComponent> ent, ref AfterAutoHandleStateEvent args) => EnsureOverlay(ent);

    [SubscribeLocalEvent]
    private void OnStrapOverlayShutdown(Entity<StrapOverlayComponent> ent, ref ComponentShutdown args) => RemoveOverlay(ent);

    private void EnsureOverlay(EntityUid strap)
    {
        if (!TryComp<StrapOverlayComponent>(strap, out var overlay))
            return;

        EnsureOverlay((strap, overlay));
    }

    private void EnsureOverlay(Entity<StrapOverlayComponent> ent)
    {
        if (!TryComp<StrapComponent>(ent, out var strap) ||
            strap.BuckledEntities.Count == 0 ||
            ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
        {
            RemoveOverlay(ent);
            return;
        }

        if (ent.Comp.Proxy is { } existing)
        {
            if (Exists(existing))
                return;

            ent.Comp.Proxy = null;
        }

        var proxy = SpawnAttachedTo(OverlayPrototype, new EntityCoordinates(ent, 0f, 0f));
        var proxySprite = Comp<SpriteComponent>(proxy);

        _sprite.SetDrawDepth((proxy, proxySprite), ent.Comp.DrawDepth);

        foreach (var data in ent.Comp.Layers)
        {
            _sprite.AddLayer((proxy, proxySprite), data, null);
        }

        ent.Comp.Proxy = proxy;
    }

    private void RemoveOverlay(Entity<StrapOverlayComponent> ent)
    {
        if (ent.Comp.Proxy is not { } proxy)
            return;

        ent.Comp.Proxy = null;
        TryQueueDel(proxy);
    }
}
