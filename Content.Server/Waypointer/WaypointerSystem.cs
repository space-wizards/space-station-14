using System.Linq;
using Content.Server.Actions;
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
public sealed class WaypointerSystem : SharedWaypointerSystem
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ActionsSystem  _actions = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaypointerComponent, ComponentInit>(OnAddition);
        SubscribeLocalEvent<WaypointerComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<WaypointerTrackableComponent, ComponentInit>(OnTrackableInit);

        SubscribeLocalEvent<WaypointerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<WaypointerComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<WaypointerComponent, MapUidChangedEvent>(OnMapChanged);
    }

    private void OnAddition(Entity<WaypointerComponent> player, ref ComponentInit args)
    {
        _actions.AddAction(player, ref player.Comp.ActionEntity, player.Comp.ActionProtoId);
    }

    private void OnRemoval(Entity<WaypointerComponent> player, ref ComponentRemove args)
    {
        _actions.RemoveAction(player.Owner, player.Comp.ActionEntity);
    }

    private void OnTrackableInit(Entity<WaypointerTrackableComponent> trackable, ref ComponentInit args)
    {
        // This might be a bit confusing, but I think this is the cheapest way to refresh overrides for new trackables.
        // I'll explain:
        // This gets all possible waypointers in the game.
        var waypointers = _prototype.GetInstances<WaypointerPrototype>();
        // This will hold all waypointers that need their overrides to be refreshed because this trackable spawned.
        var waypointersToOverride = new HashSet<ProtoId<WaypointerPrototype>>();

        // Now, we iterate through each waypointer
        foreach (var waypointer in waypointers.Values)
        {
            // And then we iterate through each component that the waypointer tracks
            foreach (var trackedComponent in waypointer.TrackedComponents.Values)
            {
                // And then check if the trackable has that tracked component
                if (!EntityManager.HasComponent(trackable, trackedComponent.Component.GetType())
                    // And of course, check if it passes the whitelist & blacklist.
                    || !_whitelist.CheckBoth(trackable, blacklist: waypointer.Blacklist, whitelist: waypointer.Whitelist))
                    continue;

                // THEN we add that WAYPOINTER to the list above.
                waypointersToOverride.Add(new ProtoId<WaypointerPrototype>(waypointer.ID));
                // If it didn't have that component, then we don't need to refresh the overrides for that one.
            }
        }

        // We get this for a check later.
        var trackXform = Transform(trackable);

        // Now we get every entity that has an active waypointer
        var waypointerQuery = EntityQueryEnumerator<WaypointerComponent, ActorComponent>();
        // We iterate through them
        while (waypointerQuery.MoveNext(out var player, out var waypointerComp, out var actorComp))
        {
            // No need to override if they don't have any waypointers.
            if (waypointerComp.WaypointerProtoIds == null)
                continue;

            // Then we iterate through every waypointer they have access to
            foreach (var waypointer in waypointerComp.WaypointerProtoIds)
            {
                // We check if they have any waypointer that can track the new trackable entity.
                if (!waypointersToOverride.Contains(waypointer))
                    continue;

                var playerXform = Transform(player);
                // Now we check if that player is on the same map as the tracked entity.
                if (trackXform.MapID != playerXform.MapID)
                    continue;

                // Then we finally override that entity for said player.
                _pvsOverride.AddSessionOverride(trackable, actorComp.PlayerSession);
                break; // No need to check other waypointers, so we break here to check for the next player.
            }
        }
    }

    private void OnPlayerAttached(Entity<WaypointerComponent> player, ref PlayerAttachedEvent args)
    {
        if (player.Comp.WaypointerProtoIds == null)
            return;

        AddOverrides(player, player.Comp.WaypointerProtoIds);
    }

    private void OnPlayerDetached(Entity<WaypointerComponent> player, ref PlayerDetachedEvent args)
    {
        if (player.Comp.WaypointerProtoIds == null)
            return;

        RemoveOverrides(player, player.Comp.WaypointerProtoIds);
    }

    private void OnMapChanged(Entity<WaypointerComponent> player, ref MapUidChangedEvent args)
    {
        // Since we only override PVS on entities on the same map, if the person switches maps, they'll need new overrides.
        RefreshOverrides(player);
    }

    /// <summary>
    /// Refreshes the Waypointer PVS Overiddes for an entity if they are controlled by a player.
    /// </summary>
    /// <param name="player">The entity to have their overrides refreshed.</param>
    [PublicAPI]
    public void RefreshOverrides(Entity<WaypointerComponent> player)
    {
        if (player.Comp.WaypointerProtoIds == null)
            return;

        RemoveOverrides(player, player.Comp.WaypointerProtoIds);
        AddOverrides(player, player.Comp.WaypointerProtoIds);
    }

    protected override void AddOverrides(EntityUid player, HashSet<ProtoId<WaypointerPrototype>> waypointers)
    {
        if (!_player.TryGetSessionByEntity(player, out var session))
            return;

        var playerXform = Transform(player);

        foreach (var waypointerProtoId in waypointers)
        {
            if (!_prototype.Resolve(waypointerProtoId, out var prototype))
                continue;

            var waypointQuery = _entity.CompRegistryQueryEnumerator(prototype.TrackedComponents);
            while (waypointQuery.MoveNext(out var target))
            {
                // Check if the target fails/passes the whitelist/blacklist.
                if (_whitelist.IsWhitelistFail(prototype.Whitelist, target)
                    || _whitelist.IsWhitelistPass(prototype.Blacklist, target))
                    continue;

                // Entities with Mapgrids somehow already work, so we exclude them. No idea why. But I fear messing with them.
                if (HasComp<MapGridComponent>(target))
                    continue;

                var targetXform = Transform(target);

                // Check if they're in the same Map. If not, don't override.
                if (targetXform.MapID == playerXform.MapID)
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
            if (!_prototype.Resolve(waypointerProtoId, out var prototype))
                continue;

            var waypointQuery = _entity.CompRegistryQueryEnumerator(prototype.TrackedComponents);
            while (waypointQuery.MoveNext(out var target))
            {
                // Entities with Mapgrids somehow already work, so we exclude them. No idea why. But I fear messing with them.
                if (HasComp<MapGridComponent>(target))
                    continue;

                _pvsOverride.RemoveSessionOverride(target, session);
            }
        }
    }
}
