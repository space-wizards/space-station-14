using Content.Shared.Shuttles.Components;
using Content.Shared.Waypointer;
using Robust.Server.GameStates;
using Robust.Shared.Player;

namespace Content.Server.Waypointer;

/// <summary>
/// This handles the PVSOverrides for the Waypointer System.
/// </summary>
public sealed class WaypointerSystem : SharedWaypointerSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WaypointerComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<WaypointerComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnRemoval(Entity<WaypointerComponent> player, ref ComponentRemove args)
    {
        if (!TryComp<ActorComponent>(player, out var actor))
            return;

        RemoveOverrides(actor.PlayerSession);
    }

    private void OnPlayerDetached(Entity<WaypointerComponent> mob, ref PlayerDetachedEvent args)
    {
        RemoveOverrides(args.Player);
    }

    private void RemoveOverrides(ICommonSession player)
    {
        var anchorQuery = EntityQueryEnumerator<StationAnchorComponent, TransformComponent>();
        while (anchorQuery.MoveNext(out var anchor, out _, out _))
        {
            _pvsOverride.RemoveSessionOverride(anchor, player);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // We need to override because the waypointers have a big range and work client-side.
        // Otherwise, the client can't draw the waypointers to the station or ATS.

        // Get everyone that has an active waypointer.
        var actorQuery = EntityQueryEnumerator<ActorComponent, WaypointerComponent, TransformComponent>();
        while (actorQuery.MoveNext(out _, out var actorComp, out _, out var actorXform))
        {
            // Get every station anchor.
            var anchorQuery = EntityQueryEnumerator<StationAnchorComponent, TransformComponent>();
            while (anchorQuery.MoveNext(out var anchor, out _, out var anchorXform))
            {
                // Check if they're in the same Map. If not, don't override.
                if (anchorXform.MapID != actorXform.MapID
                    // Also don't override if they're on the same grid. No need to find the grid you're standing on.
                    || actorXform.GridUid == anchorXform.GridUid)
                {
                    _pvsOverride.RemoveSessionOverride(anchor, actorComp.PlayerSession);
                    continue;
                }

                _pvsOverride.AddSessionOverride(anchor, actorComp.PlayerSession);
            }
        }
    }
}
