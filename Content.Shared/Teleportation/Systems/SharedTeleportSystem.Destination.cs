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
    /// <param name="user">The entity being checked.</param>
    /// <param name="target">The map coordinates to check.</param>
    /// <param name="rotation">The entity's world rotation at the destination.</param>
    /// <param name="flags">Which types of obstacles to consider.</param>
    /// <returns>Whether the destination is blocked.</returns>
    public bool IsDestinationBlocked(
        EntityUid user,
        MapCoordinates target,
        Angle rotation,
        LookupFlags flags = LookupFlags.Dynamic | LookupFlags.Static)
    {
        if (!TryComp<FixturesComponent>(user, out var fixtures))
            return false;

        if (!TryComp<PhysicsComponent>(user, out var physics))
            return false;

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
                if (other.Owner == user)
                    continue;

                if (_physics.IsCurrentlyHardCollidable((other.Owner, null, other.Comp), (user, fixtures, physics)))
                    return true;
            }
        }

        return false;
    }
}
