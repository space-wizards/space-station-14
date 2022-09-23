using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.NPC;
using Robust.Shared.Physics.Components;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    /*
     * For any custom path handlers, e.g. destroying walls, opening airlocks, etc.
     * Putting it onto steering seemed easier than trying to make a custom compound task for it.
     * I also considered task interrupts although the problem is handling stuff like pathfinding overlaps
     * Ideally we could do interrupts but that's TODO.
     */

    /*
     * TODO:
     * - Add path cap
     * - Circle cast BFS in LOS to determine targets.
     * - Store last known coordinates of X targets.
     * - Require line of sight for melee
     * - Add new behavior where they move to melee target's last known position (diffing theirs and current)
     *  then do the thing like from dishonored where it gets passed to a search system that opens random stuff.
     *
     * Also need to make sure it picks nearest obstacle path so it starts smashing in front of it.
     */


    private SteeringObstacleStatus TryHandleFlags(NPCSteeringComponent component, PathPoly poly, EntityQuery<PhysicsComponent> bodyQuery)
    {
        if (component.Flags == PathFlags.None)
            return SteeringObstacleStatus.Completed;

        // TODO: Use bodyquery
        if (!bodyQuery.TryGetComponent(component.Owner, out var body))
            return SteeringObstacleStatus.Failed;

        // TODO: Store PathFlags on the steering comp
        // and be able to re-check it.

        // TODO: Should cache the fact we're doing this somewhere.
        // See https://github.com/space-wizards/space-station-14/issues/11475
        if ((poly.Data.CollisionLayer & body.CollisionMask) != 0x0 ||
            (poly.Data.CollisionMask & body.CollisionLayer) != 0x0)
        {
            var obstacleEnts = GetObstacleEntities(poly, body.CollisionMask, body.CollisionLayer, bodyQuery);
            var isDoor = (poly.Data.Flags & PathfindingBreadcrumbFlag.Door) != 0x0;
            var isAccess = (poly.Data.Flags & PathfindingBreadcrumbFlag.Access) != 0x0;

            // Just walk into it stupid
            if (isDoor && !isAccess)
            {
                // ... At least if it's not a bump open.
                foreach (var ent in obstacleEnts)
                {
                    if (!TryComp<DoorComponent>(ent, out var door))
                        continue;

                    if (!door.BumpOpen && door.State != DoorState.Open)
                    {
                        if (door.State != DoorState.Opening)
                            _interaction.InteractionActivate(component.Owner, ent);

                        return SteeringObstacleStatus.Continuing;
                    }
                }

                return SteeringObstacleStatus.Completed;
            }

            if ((component.Flags & PathFlags.Prying) != 0x0 && isAccess && isDoor)
            {
                var doorQuery = GetEntityQuery<DoorComponent>();

                // Get the relevant obstacle
                foreach (var ent in obstacleEnts)
                {
                    if (doorQuery.TryGetComponent(ent, out var door) && door.State != DoorState.Open)
                    {
                        if (door.State != DoorState.Opening && !door.BeingPried)
                            _doors.TryPryDoor(ent, component.Owner, component.Owner, door, true);

                        return SteeringObstacleStatus.Continuing;
                    }
                }

                if (obstacleEnts.Count == 0)
                    return SteeringObstacleStatus.Completed;
            }
            // Try smashing obstacles.
            else if ((component.Flags & PathFlags.Smashing) != 0x0)
            {
                var damageQuery = GetEntityQuery<DamageableComponent>();

                foreach (var ent in obstacleEnts)
                {
                    // TODO: Validate we can damage it
                    if (damageQuery.HasComponent(ent) &&
                        TryComp<TransformComponent>(ent, out var targetXform))
                    {
                        _interaction.DoAttack(component.Owner, targetXform.Coordinates, false, targetXform.Owner);
                        return SteeringObstacleStatus.Continuing;
                    }
                }

                if (obstacleEnts.Count == 0)
                    return SteeringObstacleStatus.Completed;
            }

            return SteeringObstacleStatus.Failed;
        }

        return SteeringObstacleStatus.Completed;
    }

    private List<EntityUid> GetObstacleEntities(PathPoly poly, int mask, int layer, EntityQuery<PhysicsComponent> bodyQuery)
    {
        // TODO: Can probably re-use this from pathfinding or something
        var ents = new List<EntityUid>();

        if (!_mapManager.TryGetGrid(poly.GraphUid, out var grid))
        {
            return ents;
        }

        foreach (var ent in grid.GetLocalAnchoredEntities(poly.Box))
        {
            if (!bodyQuery.TryGetComponent(ent, out var body) ||
                !body.Hard ||
                !body.CanCollide ||
                ((body.CollisionMask & layer)) == 0x0 && (body.CollisionLayer & mask) == 0x0)
            {
                continue;
            }

            ents.Add(ent);
        }

        return ents;
    }

    private enum SteeringObstacleStatus : byte
    {
        Completed,
        Failed,
        Continuing
    }
}
