using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Physics.Components;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    /*
     * For any custom path handlers, e.g. destroying walls, opening airlocks, etc.
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
