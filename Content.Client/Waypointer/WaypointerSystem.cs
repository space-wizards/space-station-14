using Content.Shared.Actions.Components;
using Content.Shared.Waypointer;
using Content.Shared.Waypointer.Components;
using Content.Shared.Waypointer.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared.Player;

namespace Content.Client.Waypointer;

/// <summary>
/// The client-side system handles initializing the overlay, as well as removing and adding it depending on game actions.
/// </summary>
public sealed class WaypointerSystem : SharedWaypointerSystem
{
    [Dependency] private readonly IPlayerManager  _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    // This overlay cannot be generated on Initialize() - It will cause it to not fetch the station grid, causing issues.
    private WaypointerOverlay? _waypointerOverlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveWaypointerComponent, ComponentInit>(OnAddition);
        SubscribeLocalEvent<ActiveWaypointerComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<ActiveWaypointerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ActiveWaypointerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnAddition(Entity<ActiveWaypointerComponent> player, ref ComponentInit args)
    {
        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value
            || _timing.ApplyingState)
            return;

        _overlay.AddOverlay(_waypointerOverlay ??= new WaypointerOverlay());
    }

    private void OnRemoval(Entity<ActiveWaypointerComponent> player, ref ComponentRemove args)
    {
        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value
            || _timing.ApplyingState)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay ??= new WaypointerOverlay());
    }

    protected override void OnWaypointersToggled(Entity<ActionComponent> action, ref WaypointersToggledMessage args)
    {
        base.OnWaypointersToggled(action, ref args);

        _waypointerOverlay ??= new WaypointerOverlay();

        if (args.IsActive)
            _overlay.AddOverlay(_waypointerOverlay);
        else
            _overlay.RemoveOverlay(_waypointerOverlay);
    }

    private void OnPlayerAttached(Entity<ActiveWaypointerComponent> player, ref LocalPlayerAttachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.AddOverlay(_waypointerOverlay ??= new WaypointerOverlay());
    }

    private void OnPlayerDetached(Entity<ActiveWaypointerComponent> player, ref LocalPlayerDetachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay ??= new WaypointerOverlay());
    }
}
