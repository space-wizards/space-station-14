using Content.Client.Overlays;
using Content.Shared.Screech;
using Robust.Client.Graphics;

namespace Content.Client.Effects;

/// <summary>
/// This system ensures that <see cref="ScreechShockWaveOverlay"/> does not use costly queries.
/// </summary>
public sealed partial class ScreechShockWaveSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private readonly HashSet<EntityUid> _registered = [];

    public override void Initialize()
    {
        // AutoHandle fires once the component's fields have been networked
        SubscribeLocalEvent<ScreechShockWaveComponent, AfterAutoHandleStateEvent>(OnScreechShockWaveStateHandled);
        SubscribeLocalEvent<ScreechShockWaveComponent, ComponentRemove>(OnScreechShockWaveRemoved);
    }

    private void OnScreechShockWaveStateHandled(Entity<ScreechShockWaveComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var overlay = _overlayMan.GetOverlay<ScreechShockWaveOverlay>();
        // we must only pass here once
        if (!_registered.Add(ent.Owner))
            return;
        overlay.Register(ent);
    }

    private void OnScreechShockWaveRemoved(Entity<ScreechShockWaveComponent> ent, ref ComponentRemove args)
    {
        _registered.Remove(ent.Owner);
    }
}
