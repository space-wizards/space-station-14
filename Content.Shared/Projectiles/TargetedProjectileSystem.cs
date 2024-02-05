using Content.Shared.Standing;

namespace Content.Shared.Projectiles;

public sealed class TargetedProjectileSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetedProjectileComponent, ProjectileCollideEvent> (OnProjectileCollide);
    }

    /// <summary>
    ///         Cancels the collision of a projectile when it collides with an entity that is laying down but not it's target.
    /// </summary>
    private void OnProjectileCollide(EntityUid uid, TargetedProjectileComponent component, ref ProjectileCollideEvent args)
    {
        if(args.OtherEntity == component.Target ||
           !_standing.IsDown(args.OtherEntity))
            return;

        args.Cancelled = true;
    }
}
