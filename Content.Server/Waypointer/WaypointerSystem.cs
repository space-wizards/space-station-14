using System.Numerics;
using Content.Shared.Waypointer;
using Content.Shared.Waypointer.Components;
using Content.Shared.Waypointer.Events;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Waypointer;

/// <summary>
/// This handles the PVSOverrides for the Waypointer System.
/// </summary>
public sealed partial class WaypointerSystem : SharedWaypointerSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<TransformComponent> _transformQuery = default!;

    [SubscribeLocalEvent]
    private void OnMapChanged(Entity<ActiveWaypointerComponent> player, ref MapUidChangedEvent args)
    {
        // A map change requires new entities to track in the new map - This forces the server to send new data.
        player.Comp.NextUpdate = TimeSpan.Zero;
        Dirty(player);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<ActiveWaypointerComponent> player, ref PlayerAttachedEvent args)
    {
        // This enables server-side updates again, as the player is able to see the waypointers now.
        player.Comp.Active = true;
        Dirty(player);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<ActiveWaypointerComponent> player, ref PlayerDetachedEvent args)
    {
        // This disables server-side updates, as the player is unable to see the waypointers anyway.
        player.Comp.Active = false;
        Dirty(player);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveWaypointerComponent, TransformComponent>();
        while (query.MoveNext(out var player, out var waypointerComp, out var playerXform))
        {
            if (!waypointerComp.Active || waypointerComp.NextUpdate > Timing.CurTime)
                return;
            waypointerComp.NextUpdate = Timing.CurTime + waypointerComp.UpdateInterval;
            Dirty(player, waypointerComp);

            if (waypointerComp.WaypointerProtoIds == null)
            {
                RemCompDeferred<ActiveWaypointerComponent>(player);
                continue;
            }

            // We'll need to save the entity and it's position, as well as what waypointer is pointing at them.
            Dictionary<ProtoId<WaypointerPrototype>, List<(NetEntity, Vector2)>> coordinates = new ();

            foreach (var waypointer in waypointerComp.WaypointerProtoIds)
            {
                // The boolean in the dictionary describes if the waypointer is active
                if (!waypointer.Value)
                    continue;

                if (!_prototype.Resolve(waypointer.Key, out var prototype)
                    // Grids don't need their coordinates sent via server, the client already knows them.
                    || prototype.TracksGrids)
                    continue;

                List<(NetEntity, Vector2)> trackedEntities = [];

                var waypointQuery = EntityManager.CompRegistryQueryEnumerator(prototype.TrackedComponents);
                while (waypointQuery.MoveNext(out var target))
                {
                    // Check if the target fails/passes the whitelist/blacklist.
                    if (!_whitelist.CheckBoth(target, blacklist: prototype.Blacklist, whitelist: prototype.Whitelist)
                        || !_transformQuery.TryGetComponent(target, out var targetXform)
                        // Check if the target is even on the same map.
                        || targetXform.MapID != playerXform.MapID)
                        continue;

                    // Save every eligible entity in the list.
                    trackedEntities.Add((GetNetEntity(target), _transform.GetWorldPosition(target)));
                }
                // Save every list per waypointer.
                coordinates.Add(waypointer.Key, trackedEntities);
            }
            // Raise the Update Event
            var msg = new WaypointerUpdatedMessage(coordinates);
            RaiseNetworkEvent(msg, player);
        }
    }
}
