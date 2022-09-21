using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Physics.Components;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    /*
     * For any custom path handlers, e.g. destroying walls, opening airlocks, etc.
     */


    /*
     * TODO:
     * - Add "break obstacles to target" below the melee combat one. Make melee combat one require clear path
     * - Add path cap
     * - Circle cast BFS in LOS to determine targets.
     * - Store last known coordinates of X targets.
     * - Require line of sight for melee
     * - Add new behavior where they move to melee target's last known position (diffing theirs and current)
     *  then do the thing like from dishonored where it gets passed to a search system that opens random stuff.
     *
     * Also need to make sure it picks nearest obstacle path so it starts smashing in front of it.
     */


    private bool TryHandleFlags(NPCSteeringComponent component, PathPoly poly, PhysicsComponent? body = null)
    {
        if (component.Flags == PathFlags.None)
            return false;

        if (!Resolve(component.Owner, ref body, false))
            return false;

        // TODO: Store PathFlags on the steering comp
        // TODO: Need some way to say that this has been delegated out.
        // and be able to re-check it.
        if ((poly.Data.CollisionLayer & body.CollisionMask) != 0x0 ||
            (poly.Data.CollisionMask & body.CollisionLayer) != 0x0)
        {
            var combat = EnsureComp<NPCMeleeCombatComponent>(component.Owner);

            if (_mapManager.TryGetGrid(combat.Owner, out var grid))
            {
                foreach (var ent in grid.GetLocalAnchoredEntities(poly.Box))
                {

                }
            }

            return true;
        }

        return false;
    }
}
