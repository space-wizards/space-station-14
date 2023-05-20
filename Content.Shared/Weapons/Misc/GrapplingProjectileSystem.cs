using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Misc;

public sealed class GrapplingProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // TODO: Make this a generic embed component.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrapplingProjectileComponent, StartCollideEvent>(OnGrappleCollide);
    }

    private void OnGrappleCollide(EntityUid uid, GrapplingProjectileComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<ProjectileComponent>(uid, out var projectile))
            return;

        _physics.SetLinearVelocity(uid, Vector2.Zero, body: args.OurBody);
        _physics.SetBodyType(uid, BodyType.Static, body: args.OurBody);
        _transform.SetParent(uid, args.OtherEntity);

        var joint = _joints.CreateDistanceJoint(uid, projectile.Shooter, anchorA: new Vector2(0f, -0.5f));
        joint.MaxLength = joint.Length + 0.5f;
        joint.Stiffness = 1f;
        joint.MinLength = 0.1f;
        var visuals = EnsureComp<JointVisualsComponent>(uid);
        visuals.Sprite =
            new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");
    }
}
