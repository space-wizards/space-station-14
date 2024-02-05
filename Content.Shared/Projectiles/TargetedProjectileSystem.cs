using Content.Shared.Standing;

namespace Content.Shared.Projectiles;

public sealed class TargetedProjectileSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, ProjectileCollideEvent> (OnProjectileCollide);
    }

    /// <summary>
    ///     Cancels the collision of a projectile when it collides with an entity that is laying down but not it's target.
    /// </summary>
    private void OnProjectileCollide(EntityUid uid, ProjectileComponent component, ref ProjectileCollideEvent args)
    {
        if (TryComp<TargetedProjectileComponent>(uid, out var targeted) &&
            args.OtherEntity != targeted.Target &&
            _standing.IsDown(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }
}
