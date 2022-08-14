using Content.Server.NPC.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    private const int ChunkSize = 4;

    /*
     * Handles collision avoidance for NPCs.
     * There's 2 parts to this. One is storing the relevant entities (referenced via the avoidancecomponent) into chunks.
     * The other is working out our relevant avoidance velocity.
     */

    public bool CollisionAvoidanceEnabled { get; set; }= true;

    private void InitializeAvoidance()
    {

    }

    private void CollisionAvoidance((NPCSteeringComponent, ActiveNPCComponent, InputMoverComponent, TransformComponent)[] npcs)
    {
        foreach (var (steering, _, mover, xform) in npcs)
        {
            ComputeNeighbors();
            ComputeVelocity();
            // TODO: Compute velocity
        }
    }

    private void ComputeNeighbors()
    {

    }

    private void ComputeVelocity()
    {

    }
}
