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

    private WaypointerOverlay _waypointerOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _waypointerOverlay = new WaypointerOverlay();
    }

    protected override void OnMapInit(Entity<ActiveWaypointerComponent> player, ref MapInitEvent args)
    {
        base.OnMapInit(player, ref args);

        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    protected override void OnShutdown(Entity<ActiveWaypointerComponent> player, ref ComponentShutdown args)
    {
        base.OnShutdown(player, ref args);

        if (_player.LocalEntity == null || player.Owner != _player.LocalEntity.Value)
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

    /// <summary>
    /// This only gets networked to entities with the <see cref="ActiveWaypointerComponent"/>.
    /// </summary>
    /// <param name="args"></param>
    [SubscribeNetworkEvent]
    private void OnWaypointerUpdate(WaypointerUpdatedMessage args)
    {
        _waypointerOverlay.TrackedServerCoordinates = args.Coordinates;
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
