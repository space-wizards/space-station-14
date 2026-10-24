using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Teleportation.Systems;

public sealed partial class SharedTeleportSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    private readonly HashSet<Entity<PhysicsComponent>> _destinationIntersecting = new();

    /// <summary>
    /// Checks whether the entity would collide with another hard, collidable entity at the destination coordinates.
    /// </summary>
    /// <param name="target">The entity to teleport, with optional cached fixtures and physics components.</param>
    /// <param name="destination">The map coordinates to check.</param>
    /// <param name="rotation">The target's world rotation at the destination.</param>
    /// <param name="flags">Which types of obstacles to consider.</param>
    /// <returns>
    /// Whether a hard fixture at the destination overlaps an obstacle the target can collide with.
    /// Returns false if required components are missing or the entity has no collidable hard body.
    /// </returns>
    public bool IsDestinationBlocked(
        Entity<FixturesComponent?, PhysicsComponent?> target,
        MapCoordinates destination,
        Angle rotation,
        LookupFlags flags = LookupFlags.Dynamic | LookupFlags.Static)
    {
        if (!Resolve(target, ref target.Comp1, ref target.Comp2, logMissing: false))
            return false;

        var fixtures = target.Comp1;
        var physics = target.Comp2;

        if (!physics.CanCollide)
            return false;

        if (!physics.Hard)
            return false;

        var destinationTransform = new Transform(destination.Position, rotation);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            _destinationIntersecting.Clear();
            _lookup.GetEntitiesIntersecting(
                destination.MapId,
                fixture.Shape,
                destinationTransform,
                _destinationIntersecting,
                flags);

            foreach (var other in _destinationIntersecting)
            {
                if (other.Owner == target.Owner)
                    continue;

                if (_physics.IsCurrentlyHardCollidable((other.Owner, null, other.Comp), target))
                    return true;
            }
        }

        return false;
    }
}
