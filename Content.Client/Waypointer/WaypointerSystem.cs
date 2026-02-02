using Content.Client.Actions;
using Content.Shared.Waypointer;
using Content.Shared.Waypointer.Components;
using Robust.Client;
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

    private WaypointerOverlay _waypointerOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _waypointerOverlay = new WaypointerOverlay();

        SubscribeLocalEvent<WaypointerComponent, ComponentInit>(OnAddition);
        SubscribeLocalEvent<WaypointerComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<WaypointerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<WaypointerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnAddition(Entity<WaypointerComponent> mob, ref ComponentInit args)
    {
        if (_player.LocalEntity == null || mob.Owner != _player.LocalEntity.Value
            || _timing.ApplyingState)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    private void OnRemoval(Entity<WaypointerComponent> mob, ref ComponentRemove args)
    {
        if (_player.LocalEntity == null || mob.Owner != _player.LocalEntity.Value
            || _timing.ApplyingState)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }

    protected override void OnActionToggle(Entity<WaypointerComponent> mob, ref ActionToggleWaypointersEvent args)
    {
        base.OnActionToggle(mob, ref args);

        if (args.Action.Comp.Toggled)
            _overlay.RemoveOverlay(_waypointerOverlay);
        else
            _overlay.AddOverlay(_waypointerOverlay);
    }

    private void OnPlayerAttached(Entity<WaypointerComponent> mob, ref LocalPlayerAttachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    private void OnPlayerDetached(Entity<WaypointerComponent> mob, ref LocalPlayerDetachedEvent args)
    {
        if (args.Entity != _player.LocalEntity)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }
}
