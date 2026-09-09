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

    private readonly Dictionary<EntityUid, EntityUid> _applied = new();

    [SubscribeLocalEvent]
    private void OnStrapped(Entity<StrapVisualsComponent> ent, ref StrappedEvent args) => EnsureVisuals(ent);

    [SubscribeLocalEvent]
    private void OnUnstrapped(Entity<StrapVisualsComponent> ent, ref UnstrappedEvent args) => EnsureVisuals(ent);

    [SubscribeLocalEvent]
    private void OnStrapState(Entity<StrapComponent> ent, ref AfterAutoHandleStateEvent args) => EnsureVisuals(ent);

    [SubscribeLocalEvent]
    private void OnStrapVisualsShutdown(Entity<StrapVisualsComponent> ent, ref ComponentShutdown args) => RemoveVisuals(ent);

    private void EnsureVisuals(EntityUid strap)
    {
        if (!TryComp<StrapComponent>(strap, out var strapComp) ||
            strapComp.BuckledEntities.Count == 0 ||
            !TryComp<StrapVisualsComponent>(strap, out var visuals) ||
            visuals.LifeStage >= ComponentLifeStage.Stopping)
        {
            RemoveVisuals(strap);
            return;
        }

        if (_applied.TryGetValue(strap, out var existing))
        {
            if (Exists(existing))
                return;

            _applied.Remove(strap);
        }

        var proxy = SpawnAttachedTo(OverlayPrototype, new EntityCoordinates(strap, 0f, 0f));
        var proxySprite = Comp<SpriteComponent>(proxy);

        _sprite.SetDrawDepth((proxy, proxySprite), visuals.DrawDepth);

        foreach (var data in visuals.Layers)
        {
            _sprite.AddLayer((proxy, proxySprite), data, null);
        }

        _applied.Add(strap, proxy);
    }

    private void RemoveVisuals(EntityUid strap)
    {
        if (_applied.Remove(strap, out var proxy))
            TryQueueDel(proxy);
    }
}
