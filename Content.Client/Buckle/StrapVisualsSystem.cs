using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Buckle;

/// <summary>
/// Renders configured strap foreground layers on a proxy parented to the strap.
/// </summary>
public sealed partial class StrapVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly EntProtoId OverlayPrototype = "StrapVisualOverlay";

    [SubscribeLocalEvent]
    private void OnStrapped(Entity<StrapVisualsComponent> ent, ref StrappedEvent args) => EnsureOverlay(ent);

    [SubscribeLocalEvent]
    private void OnUnstrapped(Entity<StrapVisualsComponent> ent, ref UnstrappedEvent args) => EnsureOverlay(ent);

    [SubscribeLocalEvent]
    private void OnStrapState(Entity<StrapComponent> ent, ref AfterAutoHandleStateEvent args) => EnsureOverlay(ent);

    [SubscribeLocalEvent]
    private void OnStrapVisualsShutdown(Entity<StrapVisualsComponent> ent, ref ComponentShutdown args) => RemoveOverlay(ent);

    private void EnsureOverlay(EntityUid strap)
    {
        if (!TryComp<StrapVisualsComponent>(strap, out var visuals))
            return;

        EnsureOverlay((strap, visuals));
    }

    private void EnsureOverlay(Entity<StrapVisualsComponent> ent)
    {
        if (!TryComp<StrapComponent>(ent, out var strap) ||
            strap.BuckledEntities.Count == 0 ||
            ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
        {
            RemoveOverlay(ent);
            return;
        }

        if (ent.Comp.Overlay is { } existing)
        {
            if (Exists(existing))
                return;

            ent.Comp.Overlay = null;
        }

        var proxy = SpawnAttachedTo(OverlayPrototype, new EntityCoordinates(ent, 0f, 0f));
        var proxySprite = Comp<SpriteComponent>(proxy);

        _sprite.SetDrawDepth((proxy, proxySprite), ent.Comp.DrawDepth);

        foreach (var data in ent.Comp.Layers)
        {
            _sprite.AddLayer((proxy, proxySprite), data, null);
        }

        ent.Comp.Overlay = proxy;
    }

    private void RemoveOverlay(Entity<StrapVisualsComponent> ent)
    {
        if (ent.Comp.Overlay is not { } proxy)
            return;

        ent.Comp.Overlay = null;
        TryQueueDel(proxy);
    }
}
