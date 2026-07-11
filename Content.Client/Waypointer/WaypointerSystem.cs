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
public sealed partial class WaypointerSystem : SharedWaypointerSystem
{
    [Dependency] private IPlayerManager  _player = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IClientGameTiming _timing = default!;

    private WaypointerOverlay _waypointerOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _waypointerOverlay = new WaypointerOverlay();
    }

    [SubscribeLocalEvent]
    private void OnAddition(Entity<ActiveWaypointerComponent> player, ref ComponentInit args)
    {
        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value
            || _timing.ApplyingState)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    [SubscribeLocalEvent]
    private void OnRemoval(Entity<ActiveWaypointerComponent> player, ref ComponentRemove args)
    {
        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value
            || _timing.ApplyingState)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<ActiveWaypointerComponent> player, ref LocalPlayerAttachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<ActiveWaypointerComponent> player, ref LocalPlayerDetachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }

    protected override void OnWaypointersToggled(Entity<ActionComponent> action, ref WaypointersToggledMessage args)
    {
        base.OnWaypointersToggled(action, ref args);

        if (args.IsActive)
            _overlay.AddOverlay(_waypointerOverlay);
        else
            _overlay.RemoveOverlay(_waypointerOverlay);
    }
}
