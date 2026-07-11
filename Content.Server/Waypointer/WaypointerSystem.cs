using System.Linq;
using Content.Shared.Waypointer;
using Content.Shared.Waypointer.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Waypointer;

/// <summary>
/// This handles the PVSOverrides for the Waypointer System.
/// </summary>
public sealed partial class WaypointerSystem : SharedWaypointerSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void OnAddition(Entity<ActiveWaypointerComponent> player, ref ComponentInit args)
    {
        Actions.AddAction(player, ref player.Comp.ActionEntity, player.Comp.ActionProtoId);
    }

    [SubscribeLocalEvent]
    private void OnRemoval(Entity<ActiveWaypointerComponent> player, ref ComponentRemove args)
    {
        Actions.RemoveAction(player.Owner, player.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnTrackableInit(Entity<WaypointerTrackableComponent> trackable, ref ComponentInit args)
    {
        // This might be a bit confusing, but I think this is the cheapest way to refresh overrides for new trackables.
        // I'll explain:
        // This gets all possible waypointers in the game.
        var waypointers = ProtoMan.GetInstances<WaypointerPrototype>();
        // This will hold all waypointers that need their overrides to be refreshed because this trackable spawned.
        var waypointersToOverride = new HashSet<ProtoId<WaypointerPrototype>>();

        foreach (var waypointer in waypointers.Values)
        {
            // We iterate through each component that the waypointer tracks
            foreach (var trackedComponent in waypointer.TrackedComponents.Values)
            {
                // And then check if the trackable has that tracked component & passes whitelist
                if (!HasComp(trackable, trackedComponent.Component.GetType())
                    || !_whitelist.CheckBoth(trackable, blacklist: waypointer.Blacklist, whitelist: waypointer.Whitelist))
                    continue;

                // THEN we add that WAYPOINTER to the list above.
                waypointersToOverride.Add(new ProtoId<WaypointerPrototype>(waypointer.ID));
            }
        }

        // Map for the trackable entity, used for mapchecks later.
        var trackableMap = Transform(trackable).MapID;

        var waypointerQuery = AllEntityQuery<ActiveWaypointerComponent, ActorComponent>();
        while (waypointerQuery.MoveNext(out var player, out var waypointerComp, out var actorComp))
        {
            // No need to override if they don't have any waypointers.
            if (waypointerComp.WaypointerProtoIds == null)
                continue;

            foreach (var waypointer in waypointerComp.WaypointerProtoIds.Keys)
            {
                // We check if they have any waypointer that can track the new trackable entity.
                if (!waypointersToOverride.Contains(waypointer))
                    continue;

                if (trackableMap != Transform(player).MapID)
                    continue;

                // Then we finally override that entity for said player.
                _pvsOverride.AddSessionOverride(trackable, actorComp.PlayerSession);
                break; // No need to check other waypointers, so we break here to check for the next player.
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<ActiveWaypointerComponent> player, ref PlayerAttachedEvent args)
    {
        if (player.Comp.WaypointerProtoIds == null)
            return;

        AddOverrides(player, player.Comp.WaypointerProtoIds.Keys.ToHashSet());
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<ActiveWaypointerComponent> player, ref PlayerDetachedEvent args)
    {
        if (player.Comp.WaypointerProtoIds == null)
            return;

        RemoveOverrides(player, player.Comp.WaypointerProtoIds.Keys.ToHashSet());
    }

    [SubscribeLocalEvent]
    private void OnMapChanged(Entity<ActiveWaypointerComponent> player, ref MapUidChangedEvent args)
    {
        // Since we only override PVS on entities on the same map, if the person switches maps, they'll need new overrides.
        RefreshOverrides(player);
    }

    /// <summary>
    /// Refreshes the Waypointer PVS Overiddes for an entity if they are controlled by a player.
    /// </summary>
    /// <param name="player">The entity to have their overrides refreshed.</param>
    [PublicAPI]
    public void RefreshOverrides(Entity<ActiveWaypointerComponent> player)
    {
        if (player.Comp.WaypointerProtoIds == null)
            return;

        RemoveOverrides(player, player.Comp.WaypointerProtoIds.Keys.ToHashSet());
        AddOverrides(player, player.Comp.WaypointerProtoIds.Keys.ToHashSet());
    }

    protected override void AddOverrides(EntityUid player, HashSet<ProtoId<WaypointerPrototype>> waypointers)
    {
        if (!_player.TryGetSessionByEntity(player, out var session))
            return;

        var playerMap = Transform(player).MapID;

        foreach (var waypointerProtoId in waypointers)
        {
            if (!ProtoMan.Resolve(waypointerProtoId, out var prototype))
                continue;

            var waypointQuery = EntityManager.CompRegistryQueryEnumerator(prototype.TrackedComponents);
            while (waypointQuery.MoveNext(out var target))
            {
                // Grids somehow already work, so we exclude them. No idea why. But I fear messing with them.
                if (HasComp<MapGridComponent>(target)
                    // If it doesn't have the trackable component either, it doesn't work.
                    || HasComp<WaypointerTrackableComponent>(target)
                    // Check if the target fails/passes the whitelist/blacklist.
                    || _whitelist.CheckBoth(target, whitelist: prototype.Whitelist, blacklist: prototype.Blacklist))
                    continue;

                if (playerMap == Transform(target).MapID)
                    _pvsOverride.AddSessionOverride(target, session);
            }
        }
    }

    protected override void RemoveOverrides(EntityUid player, HashSet<ProtoId<WaypointerPrototype>> waypointers)
    {
        if (!_player.TryGetSessionByEntity(player, out var session))
            return;

        foreach (var waypointerProtoId in waypointers)
        {
            if (!ProtoMan.Resolve(waypointerProtoId, out var prototype))
                continue;

            var waypointQuery = EntityManager.CompRegistryQueryEnumerator(prototype.TrackedComponents);
            while (waypointQuery.MoveNext(out var target))
            {
                // Grids somehow already work, so we exclude them. No idea why. But I fear messing with them.
                if (HasComp<MapGridComponent>(target))
                    continue;

                _pvsOverride.RemoveSessionOverride(target, session);
            }
        }
    }
}
