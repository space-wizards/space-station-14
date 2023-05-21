using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Misc;

public sealed class GrapplingProjectileSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;

    public const string GrapplingJoint = "grappling";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrapplingProjectileComponent, ProjectileEmbedEvent>(OnGrappleCollide);
        SubscribeLocalEvent<GrapplingGunComponent, EntParentChangedMessage>(OnGunParentChange);
        SubscribeLocalEvent<GrapplingGunComponent, ActivateInWorldEvent>(OnGunActivate);
        SubscribeLocalEvent<CanWeightlessMoveEvent>(OnWeightlessMove);
    }

    private void OnWeightlessMove(ref CanWeightlessMoveEvent ev)
    {
        if (ev.CanMove || !TryComp<JointComponent>(ev.Uid, out var jointComp) || !jointComp.GetJoints.ContainsKey(GrapplingJoint))
            return;

        ev.CanMove = true;
    }

    private void OnGunActivate(EntityUid uid, GrapplingGunComponent component, ActivateInWorldEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _joints.RemoveJoint(Transform(uid).ParentUid, GrapplingJoint);
    }

    private void OnGunParentChange(EntityUid uid, GrapplingGunComponent component, ref EntParentChangedMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<JointComponent>(args.OldParent, out var parentJoints))
            return;

        // Copy the joint to the gun.
        if (parentJoints.GetJoints.TryGetValue(GrapplingJoint, out var oldJoint) && oldJoint is DistanceJoint distance && !_containers.IsEntityOrParentInContainer(uid))
        {
            var joint = _joints.CreateDistanceJoint(oldJoint.BodyAUid, uid, anchorA: oldJoint.LocalAnchorA, id: GrapplingJoint);
            joint.MaxLength = distance.MaxLength;
            joint.Length = distance.Length;
            joint.Stiffness = distance.Stiffness;
            joint.MinLength = distance.MinLength;
        }

        _joints.RemoveJoint(args.OldParent.Value, GrapplingJoint);
    }

    private void OnGrappleCollide(EntityUid uid, GrapplingProjectileComponent component, ref ProjectileEmbedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var joint = _joints.CreateDistanceJoint(uid, args.Shooter, anchorA: new Vector2(0f, -0.5f), id: GrapplingJoint);
        joint.MaxLength = joint.Length + 0.5f;
        joint.Stiffness = 1f;
        joint.MinLength = 0.1f;
        var visuals = EnsureComp<JointVisualsComponent>(uid);
        visuals.Sprite =
            new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");

        visuals.OffsetA = new Vector2(0f, 0.5f);
    }
}
