using Content.Server.NPC.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Map;
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
        SubscribeLocalEvent<GridInitializeEvent>(OnAvoidanceGridInit);
        SubscribeLocalEvent<GridRemovalEvent>(OnAvoidanceGridRemoval);
        SubscribeLocalEvent<MapChangedEvent>(OnAvoidanceMapInit);
        SubscribeLocalEvent<NPCAvoidanceComponent, ComponentStartup>(OnAvoidanceStartup);
        SubscribeLocalEvent<NPCAvoidanceComponent, ComponentShutdown>(OnAvoidanceShutdown);
        SubscribeLocalEvent<NPCAvoidanceComponent, MoveEvent>(OnAvoidanceMove);
        SubscribeLocalEvent<NPCAvoidanceComponent, CollisionChangeEvent>(OnAvoidanceCollision);
    }

    private Vector2 GetAvoidanceVector(NPCSteeringComponent component)
    {
        // TODO: Get relevant VOs in front of us
        // TODO: Combined VO

        return Vector2.Zero;
    }

    private float CollisionTime(EntityUid uidA, EntityUid uidB)
    {
        // TODO: NPCSteeringComponent needs to store its current target on the path on the component
        // TODO: Need

        // TODO:
        // 1. Don't avoid stuff behind us (120 degree arc?)
        // 2. Avoidance priorities (i.e. dragon doesn't avoid carps)
        // 3. Obstacle beyond target coordinates

        // Conservative collision time is roughly d / v
        // TODO Based on <Horizon Article sloth source it>
        var xformA = Transform(uidA);
        var xformB = Transform(uidB);
        var moveSpeedA = GetSprintSpeed(uidA);
        var velocityB = Vector2.Zero;

        if (TryComp<PhysicsComponent>(uidB, out var bodyB))
            velocityB = bodyB.LinearVelocity;

        var positionA = xformA.LocalPosition;
        var positionB = xformB.LocalPosition;

        // TODO:
        var radius = 0.35f;

        var gap = (positionB - positionA).Length - (radius + radius);
        var approachSpeed = moveSpeedA + Vector2.Dot(velocityB, positionA - positionB);

        return gap / approachSpeed;
    }

    /// <summary>
    /// Returns the cone we need to avoid to not collide with entity B.
    /// </summary>
    private VelocityObstacle GetVelocityObstacle(EntityUid uidA, EntityUid uidB)
    {
        // Essentially we need to get a cone from entity A to entity B
        // The edges of the cone are offset by both their radii

        // Can just use pythagoras to get the offset
        // TODO: Custom
        var combinedRadii = 0.7f;
        

        var xformA = Transform(uidA);
        var xformB = Transform(uidB);


    }

    #region Lookup

    private void OnAvoidanceMapInit(MapChangedEvent ev)
    {
        if (ev.Created)
        {
            EnsureComp<NPCAvoidanceLookupComponent>(_mapManager.GetMapEntityId(ev.Map));
        }
        else
        {
            RemComp<NPCAvoidanceLookupComponent>(_mapManager.GetMapEntityId(ev.Map));
        }
    }

    private void OnAvoidanceGridInit(GridInitializeEvent ev)
    {
        EnsureComp<NPCAvoidanceLookupComponent>(ev.EntityUid);
    }

    private void OnAvoidanceGridRemoval(GridRemovalEvent ev)
    {
        RemComp<NPCAvoidanceLookupComponent>(ev.EntityUid);
    }

    private Vector2i GetChunkIndices(Vector2 position)
    {
        return (position / ChunkSize).Floored();
    }

    public void SetEnabled(NPCAvoidanceComponent component, bool enabled)
    {
        if (component.Enabled.Equals(enabled))
            return;

        component.Enabled = enabled;

        if (enabled)
        {
            AddToLookup(component.Owner, Transform(component.Owner).Coordinates);
        }
        else
        {
            RemoveFromLookup(component.Owner, Transform(component.Owner).Coordinates);
        }
    }

    private void AddToLookup(EntityUid uid, EntityCoordinates coordinates)
    {
        if (!TryComp<NPCAvoidanceLookupComponent>(coordinates.EntityId, out var lookup))
            return;

        var chunk = lookup.Chunks.GetOrNew(GetChunkIndices(coordinates.Position));
        chunk.Entities.Add(uid);
    }

    private void RemoveFromLookup(EntityUid uid, EntityCoordinates coordinates)
    {
        if (!TryComp<NPCAvoidanceLookupComponent>(coordinates.EntityId, out var lookup))
            return;

        var indices = GetChunkIndices(coordinates.Position);

        if (!lookup.Chunks.TryGetValue(indices, out var chunk))
            return;

        chunk.Entities.Remove(uid);

        if (chunk.Entities.Count == 0)
        {
            lookup.Chunks.Remove(indices);
        }
    }

    private void OnAvoidanceStartup(EntityUid uid, NPCAvoidanceComponent component, ComponentStartup args)
    {
        if (component.Enabled)
            return;

        AddToLookup(uid, Transform(uid).Coordinates);
    }

    private void OnAvoidanceShutdown(EntityUid uid, NPCAvoidanceComponent component, ComponentShutdown args)
    {
        if (component.Enabled)
            return;

        RemoveFromLookup(uid, Transform(uid).Coordinates);
    }

    private void OnAvoidanceMove(EntityUid uid, NPCAvoidanceComponent component, ref MoveEvent args)
    {
        if (component.Enabled)
            return;

        RemoveFromLookup(uid, args.OldPosition);
        AddToLookup(uid, args.NewPosition);
    }

    private void OnAvoidanceCollision(EntityUid uid, NPCAvoidanceComponent component, CollisionChangeEvent args)
    {
        component.Enabled = args.CanCollide;
    }

    #endregion

    private struct VelocityObstacle
    {
        public Vector2 Edge1;
        public Vector2 Edge2;

        public Vector2 Origin;
    }
}
