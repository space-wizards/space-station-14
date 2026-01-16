using Content.Shared.CombatMode;
using Content.Shared.Waypointer;
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
    [Dependency] private readonly IPlayerManager  _playerManager = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private WaypointerOverlay _waypointerOverlay = default!;

    public override void Initialize()
    {
        _waypointerOverlay = new WaypointerOverlay();

        SubscribeLocalEvent<WaypointerComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<WaypointerComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<WaypointerComponent, ToggleCombatActionEvent>(OnCombatToggle);

        SubscribeLocalEvent<WaypointerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<WaypointerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnCompInit(Entity<WaypointerComponent> mob, ref ComponentInit args)
    {
        if (_playerManager.LocalEntity == null || mob.Owner != _playerManager.LocalEntity.Value)
            return;

        _overlayManager.AddOverlay(_waypointerOverlay);
    }

    private void OnRemoval(Entity<WaypointerComponent> mob, ref ComponentRemove args)
    {
        if (_playerManager.LocalEntity == null || mob.Owner != _playerManager.LocalEntity.Value)
            return;

        _overlayManager.RemoveOverlay(_waypointerOverlay);
    }

    private void OnCombatToggle(Entity<WaypointerComponent> combatant, ref ToggleCombatActionEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // Somehow, args.Toggle does not change from false to true whenever. So we are using this.
        // When combat mode is on, turn off the overlay, so it's less distraction.
        if (args.Action.Comp.Toggled)
            _overlayManager.AddOverlay(_waypointerOverlay);
        else
            _overlayManager.RemoveOverlay(_waypointerOverlay);
    }

    private void OnPlayerAttached(Entity<WaypointerComponent> mob, ref LocalPlayerAttachedEvent args)
    {
        if (args.Entity != _playerManager.LocalEntity)
            return;

        _overlayManager.AddOverlay(_waypointerOverlay);
    }

    private void OnPlayerDetached(Entity<WaypointerComponent> mob, ref LocalPlayerDetachedEvent args)
    {
        if (args.Entity != _playerManager.LocalEntity)
            return;

        _overlayManager.RemoveOverlay(_waypointerOverlay);
    }
}
