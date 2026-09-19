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
    /// Checks whether the entity would collide with another hard, collidable entity at the target coordinates.
    /// </summary>
    /// <param name="user">The entity being checked, with optional cached fixtures and physics components.</param>
    /// <param name="target">The map coordinates to check.</param>
    /// <param name="rotation">The entity's world rotation at the destination.</param>
    /// <param name="flags">Which types of obstacles to consider.</param>
    /// <returns>Whether the destination is blocked. Returns false if either required component is missing.</returns>
    public bool IsDestinationBlocked(
        Entity<FixturesComponent?, PhysicsComponent?> user,
        MapCoordinates target,
        Angle rotation,
        LookupFlags flags = LookupFlags.Dynamic | LookupFlags.Static)
    {
        if (!Resolve(user, ref user.Comp1, ref user.Comp2, logMissing: false))
            return false;

        var fixtures = user.Comp1;
        var physics = user.Comp2;

        if (!physics.CanCollide)
            return false;

        if (!physics.Hard)
            return false;

        var destinationTransform = new Transform(target.Position, rotation);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            _destinationIntersecting.Clear();
            _lookup.GetEntitiesIntersecting(
                target.MapId,
                fixture.Shape,
                destinationTransform,
                _destinationIntersecting,
                flags);

            foreach (var other in _destinationIntersecting)
            {
                if (other.Owner == user.Owner)
                    continue;

                if (_physics.IsCurrentlyHardCollidable((other.Owner, null, other.Comp), user))
                    return true;
            }
        }

        return false;
    }
}
