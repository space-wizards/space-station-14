using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Buckle;

/// <summary>
/// Renders extra visual layers while this strap is occupied.
/// </summary>
public sealed partial class StrapOverlaySystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly EntProtoId OverlayPrototype = "StrapOverlayVisual";

    private readonly Dictionary<EntityUid, EntityUid> _overlays = new();

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
    private void OnStrapOverlayShutdown(Entity<StrapOverlayComponent> ent, ref ComponentShutdown args) => RemoveOverlay(ent);

    private void EnsureOverlay(Entity<StrapOverlayComponent> ent)
    {
        if (_overlays.TryGetValue(ent, out var existing))
        {
            if (Exists(existing))
                return;

            _overlays.Remove(ent);
        }

        var proxy = SpawnAttachedTo(OverlayPrototype, new EntityCoordinates(ent, 0f, 0f));
        var proxySprite = Comp<SpriteComponent>(proxy);

        _sprite.SetDrawDepth((proxy, proxySprite), ent.Comp.DrawDepth);

        foreach (var data in ent.Comp.Layers)
        {
            _sprite.AddLayer((proxy, proxySprite), data, null);
        }

        _overlays.Add(ent, proxy);
    }

    private void RemoveOverlay(Entity<StrapOverlayComponent> ent)
    {
        if (_overlays.Remove(ent, out var proxy))
            TryQueueDel(proxy);
    }
}
