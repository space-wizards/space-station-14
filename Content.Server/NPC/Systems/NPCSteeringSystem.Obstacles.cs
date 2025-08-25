using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.NPC;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Utility;
using ClimbingComponent = Content.Shared.Climbing.Components.ClimbingComponent;

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


    private SteeringObstacleStatus TryHandleFlags(EntityUid uid, NPCSteeringComponent component, PathPoly poly)
    {
        DebugTools.Assert(!poly.Data.IsFreeSpace);
        // TODO: Store PathFlags on the steering comp
        // and be able to re-check it.

        var layer = 0;
        var mask = 0;

        if (TryComp<FixturesComponent>(uid, out var manager))
        {
            (layer, mask) = _physics.GetHardCollision(uid, manager);
        }
        else
        {
            return SteeringObstacleStatus.Failed;
        }

        // TODO: Should cache the fact we're doing this somewhere.
        // See https://github.com/space-wizards/space-station-14/issues/11475
        if ((poly.Data.CollisionLayer & mask) == 0x0 &&
            (poly.Data.CollisionMask & layer) == 0x0)
            return SteeringObstacleStatus.Completed;

        var id = component.DoAfterId;

        // Still doing what we were doing before.
        var doAfterStatus = _doAfter.GetStatus(id);

        switch (doAfterStatus)
        {
            case DoAfterStatus.Running:
                return SteeringObstacleStatus.Continuing;
            case DoAfterStatus.Cancelled:
                return SteeringObstacleStatus.Failed;
        }

        if (!TryComp<MapGridComponent>(poly.GraphUid, out var grid))
            return SteeringObstacleStatus.Completed;

        var obstacleEnts = _mapSystem.GetLocalAnchoredEntities(poly.GraphUid, grid, poly.Box).ToHashSet();
        FilterObstacleEntities((uid, component), mask, layer, obstacleEnts);

        // Nothing actually near us.
        if (obstacleEnts.Count == 0)
            return SteeringObstacleStatus.Completed;

        var isDoor = (poly.Data.Flags & PathfindingBreadcrumbFlag.Door) != 0x0;
        var isClimbable = (poly.Data.Flags & PathfindingBreadcrumbFlag.Climb) != 0x0;

        // Just walk into it stupid
        if (isDoor)
        {
            foreach (var ent in obstacleEnts)
            {
                // Includes checking if we have access:
                if (!CanHandleDoor((uid, component), poly.Data.Flags, ent, false))
                    continue;

                // Interacts are bit nicer than bumps, so try interacting regardless
                _interaction.InteractionActivate(uid, ent);

                return SteeringObstacleStatus.Continuing;
            }

            // Couldn't normal-open the door, and can't pry. Give up.
            if ((component.Flags & PathFlags.Prying) == 0x0)
                return SteeringObstacleStatus.Failed;

            foreach (var ent in obstacleEnts)
            {
                if (!CanHandleDoor((uid, component), poly.Data.Flags, ent))
                    continue;

                // Should be able to pry from CanHandleDoor:
                // TODO: Use the verb.
                _pryingSystem.TryPry(ent, uid, out id, uid);

                component.DoAfterId = id;
                return SteeringObstacleStatus.Continuing;
            }
        }
        // Try climbing obstacles
        else if ((component.Flags & PathFlags.Climbing) != 0x0 && isClimbable)
        {
            if (!TryComp<ClimbingComponent>(uid, out var climbing))
                return SteeringObstacleStatus.Failed;

            if (climbing.IsClimbing)
                return SteeringObstacleStatus.Completed;

            if (climbing.NextTransition != null)
                return SteeringObstacleStatus.Continuing;

            // Get the relevant obstacle
            foreach (var ent in obstacleEnts)
            {
                if (CanHandleClimb((uid, climbing), ent, out var climbable) &&
                    _climb.TryClimb(uid, uid, ent, out id, climbable, climbing))
                {
                    component.DoAfterId = id;
                    return SteeringObstacleStatus.Continuing;
                }
            }
        }
        // Try smashing obstacles.
        else if ((component.Flags & PathFlags.Smashing) != 0x0)
        {
            // Check we have a weapon, can (probably) swing it, and have combat mode.
            if (!_melee.TryGetWeapon(uid, out var weaponUid, out var weaponComp) ||
                weaponComp.NextAttack > _timing.CurTime ||
                !TryComp<CombatModeComponent>(uid, out var combatMode))
                return SteeringObstacleStatus.Failed;

            _combat.SetInCombatMode(uid, true, combatMode);

            // TODO: This is a hack around grilles and windows.
            var shuffledEnts = obstacleEnts.ToList();
            _random.Shuffle(shuffledEnts);

            var attackResult = false;
            foreach (var ent in shuffledEnts)
            {
                // TODO: Validate we can damage it
                if (!_destructibleQuery.HasComponent(ent))
                    continue;

                attackResult = _melee.AttemptLightAttack(uid, weaponUid, weaponComp, ent);
                break;
            }

            _combat.SetInCombatMode(uid, false, combatMode);

            // Blocked or the likes?
            if (!attackResult)
                return SteeringObstacleStatus.Failed;

            return SteeringObstacleStatus.Continuing;
        }

        return SteeringObstacleStatus.Failed;
    }

    private enum SteeringObstacleStatus : byte
    {
        Completed,
        Failed,
        Continuing
    }
}
