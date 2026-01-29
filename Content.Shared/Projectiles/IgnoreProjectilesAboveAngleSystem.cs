using Robust.Shared.Physics.Events;

namespace Content.Shared.Projectiles;

public sealed class IgnoreProjectilesAboveAngleSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgnoreProjectilesAboveAngleComponent, PreventCollideEvent>(OnPreventCollide);
    }

    /// <summary>
    /// Checks if a projectile should pass through an entity with <see cref="IgnoreProjectilesAboveAngleComponent"/>
    /// </summary>
    public bool IgnoreAboveAngleCheck(Entity<IgnoreProjectilesAboveAngleComponent> targetUid,
        EntityUid projectile,
        EntityUid? shooter)
    {
        if (shooter is { } projShooter)
        {
            var shooterPosition = _transform.GetWorldPosition(projShooter);
            var targetEntityPosition = _transform.GetWorldPosition(targetUid);

            if (!(shooterPosition - targetEntityPosition).IsShorterThan((float)targetUid.Comp.MaximumDistance))
            {
                return false;
            }
        }

        var projectileAngle = _transform.GetWorldRotation(projectile);
        var targetEntityAngle = _transform.GetWorldRotation(targetUid);

        if (targetUid.Comp.Backwards)
        {
            projectileAngle = projectileAngle.Opposite();
        }

        var angleDifference = projectileAngle - targetEntityAngle;

        return (double.Abs(angleDifference.Reduced().Theta) < targetUid.Comp.Angle.Theta) == targetUid.Comp.Reversed;
    }

    private void OnPreventCollide(Entity<IgnoreProjectilesAboveAngleComponent> uid, ref PreventCollideEvent args)
    {
        if (TryComp(args.OtherEntity, out ProjectileComponent? component) &&
            IgnoreAboveAngleCheck(uid, args.OtherEntity, component.Shooter))
        {
            args.Cancelled = true;
        }
    }
}
