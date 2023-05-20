using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Misc;

public sealed class GrapplingProjectileSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;

    public const string GrapplingJoint = "grappling";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrapplingProjectileComponent, ProjectileEmbedEvent>(OnGrappleCollide);
    }

    private void OnGrappleCollide(EntityUid uid, GrapplingProjectileComponent component, ref ProjectileEmbedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var joint = _joints.CreateDistanceJoint(uid, args.Weapon, anchorA: new Vector2(0f, -0.5f), id: GrapplingJoint);
        joint.MaxLength = joint.Length + 0.5f;
        joint.Stiffness = 1f;
        joint.MinLength = 0.1f;
        var visuals = EnsureComp<JointVisualsComponent>(uid);
        visuals.Sprite =
            new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");

        visuals.OffsetA = new Vector2(0f, 0.5f);
    }
}
