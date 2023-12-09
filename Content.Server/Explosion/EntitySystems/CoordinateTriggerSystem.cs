using Content.Server.Explosion.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Explosion.EntitySystems;

public sealed class CoordinateTriggerSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    // Tile distance from cursor where projectile will still explode if colliding.
    private const float AimCompensation = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TriggerWhenReachingCoordinatesComponent, StartCollideEvent>(OnStartCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<TriggerWhenReachingCoordinatesComponent>();

        while (query.MoveNext(out var uid, out var aimCoordinates))
        {

            if (aimCoordinates.Origin != null)
            {
                var xform = Transform(uid);
                var bulletLocation = xform.Coordinates.ToMapPos(EntityManager, _transformSystem);
                var bulletOrigin = aimCoordinates.Origin.Value.Position;
                var bulletDestination = aimCoordinates.Destination;

                // Checks if shot projectile has reached the destination point
                if (bulletLocation.X < bulletDestination.X &&
                    bulletOrigin.X > bulletDestination.X)
                {
                    _triggerSystem.Trigger(uid);
                }
                else if (bulletLocation.X > bulletDestination.X &&
                         bulletOrigin.X < bulletDestination.X)
                {
                    _triggerSystem.Trigger(uid);
                }
            }
        }
    }

    /// <summary>
    /// Triggers the projectile if it starts colliding with an entity close to the aimed location.
    /// This compensates for hitboxes larger than the sprite and allows people to aim on top of stationary targets.
    /// </summary>
    /// <param name="uid">EntityUid of the projectile </param>
    /// <param name="comp">TriggerWhenReachingCoordinatesComponent</param>
    /// <param name="args">arguments related to StartcollideEvent</param>
    private void OnStartCollide(EntityUid uid, TriggerWhenReachingCoordinatesComponent comp,
        ref StartCollideEvent args)
    {
        var xform = Transform(uid);
        var projectileLocation = xform.Coordinates.ToMapPos(EntityManager, _transformSystem);
        var projectileDestination = comp.Destination;

        if (projectileLocation.X - projectileDestination.X > -AimCompensation &&
            projectileLocation.X - projectileDestination.X < AimCompensation &&
            projectileLocation.Y - projectileDestination.Y > -AimCompensation &&
            projectileLocation.Y - projectileDestination.Y < AimCompensation)
        {
            _triggerSystem.Trigger(uid);
        }
    }
}
