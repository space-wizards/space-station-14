using Content.Server.CombatMode;
using Content.Server.Destructible;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Doors.Components;
using Content.Shared.NPC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

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
        DebugTools.Assert(!poly.Data.IsFreeSpace);
        // TODO: Store PathFlags on the steering comp
        // and be able to re-check it.

        var layer = 0;
        var mask = 0;

        if (TryComp<FixturesComponent>(component.Owner, out var manager))
        {
            (layer, mask) = _physics.GetHardCollision(component.Owner, manager);
        }
        else
        {
            return SteeringObstacleStatus.Failed;
        }

        // TODO: Should cache the fact we're doing this somewhere.
        // See https://github.com/space-wizards/space-station-14/issues/11475
        if ((poly.Data.CollisionLayer & mask) != 0x0 ||
            (poly.Data.CollisionMask & layer) != 0x0)
        {
            var obstacleEnts = new List<EntityUid>();

            GetObstacleEntities(poly, mask, layer, bodyQuery, obstacleEnts);
            var isDoor = (poly.Data.Flags & PathfindingBreadcrumbFlag.Door) != 0x0;
            var isAccess = (poly.Data.Flags & PathfindingBreadcrumbFlag.Access) != 0x0;

            // Just walk into it stupid
            if (isDoor && !isAccess)
            {
                var doorQuery = GetEntityQuery<DoorComponent>();

                // ... At least if it's not a bump open.
                foreach (var ent in obstacleEnts)
                {
                    if (!doorQuery.TryGetComponent(ent, out var door))
                        continue;

                    if (!door.BumpOpen && (component.Flags & PathFlags.Interact) != 0x0)
                    {
                        if (door.State != DoorState.Opening)
                        {
                            _interaction.InteractionActivate(component.Owner, ent);
                            return SteeringObstacleStatus.Continuing;
                        }
                    }
                    else
                    {
                        return SteeringObstacleStatus.Failed;
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
                        // TODO: Use the verb.
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
                var meleeWeapon = _melee.GetWeapon(component.Owner);

                if (meleeWeapon != null && meleeWeapon.NextAttack <= _timing.CurTime && TryComp<CombatModeComponent>(component.Owner, out var combatMode))
                {
                    combatMode.IsInCombatMode = true;
                    var destructibleQuery = GetEntityQuery<DestructibleComponent>();

                    // TODO: This is a hack around grilles and windows.
                    _random.Shuffle(obstacleEnts);

                    foreach (var ent in obstacleEnts)
                    {
                        // TODO: Validate we can damage it
                        if (destructibleQuery.HasComponent(ent))
                        {
                            _melee.AttemptLightAttack(component.Owner, meleeWeapon, ent);
                            break;
                        }
                    }

                    combatMode.IsInCombatMode = false;

                    if (obstacleEnts.Count == 0)
                        return SteeringObstacleStatus.Completed;

                    return SteeringObstacleStatus.Continuing;
                }
            }

            return SteeringObstacleStatus.Failed;
        }

        return SteeringObstacleStatus.Completed;
    }

    private void GetObstacleEntities(PathPoly poly, int mask, int layer, EntityQuery<PhysicsComponent> bodyQuery,
        List<EntityUid> ents)
    {
        // TODO: Can probably re-use this from pathfinding or something
        if (!_mapManager.TryGetGrid(poly.GraphUid, out var grid))
        {
            return;
        }

        foreach (var ent in grid.GetLocalAnchoredEntities(poly.Box))
        {
            if (!bodyQuery.TryGetComponent(ent, out var body) ||
                !body.Hard ||
                !body.CanCollide ||
                (body.CollisionMask & layer) == 0x0 && (body.CollisionLayer & mask) == 0x0)
            {
                continue;
            }

            ents.Add(ent);
        }
    }

    private enum SteeringObstacleStatus : byte
    {
        Completed,
        Failed,
        Continuing
    }
}
