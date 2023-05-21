using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Misc;

public abstract class SharedGrapplingGunSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;

    public const string GrapplingJoint = "grappling";

    public const float ReelRate = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrapplingProjectileComponent, ProjectileEmbedEvent>(OnGrappleCollide);
        SubscribeLocalEvent<CanWeightlessMoveEvent>(OnWeightlessMove);
        SubscribeAllEvent<RequestGrapplingReelMessage>(OnGrapplingReel);

        SubscribeLocalEvent<GrapplingGunComponent, EntParentChangedMessage>(OnGunParentChange);
        SubscribeLocalEvent<GrapplingGunComponent, ActivateInWorldEvent>(OnGunActivate);
        SubscribeLocalEvent<GrapplingGunComponent, HandDeselectedEvent>(OnGrapplingDeselected);
        SubscribeLocalEvent<GrapplingGunComponent, AfterAutoHandleStateEvent>(OnGrapplingState);
    }

    private void OnGrapplingState(EntityUid uid, GrapplingGunComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!component.Reeling)
        {
            component.Stream?.Stop();
        }
    }

    private void OnGrapplingDeselected(EntityUid uid, GrapplingGunComponent component, HandDeselectedEvent args)
    {
        SetReeling(uid, component, false, args.User);
    }

    private void OnGrapplingReel(RequestGrapplingReelMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;

        if (!TryComp<CombatModeComponent>(player, out var combatMode) ||
            !combatMode.IsInCombatMode)
        {
            return;
        }

        if (!TryComp<JointComponent>(player, out var jointComp) ||
            !jointComp.GetJoints.ContainsKey(GrapplingJoint))
        {
            return;
        }

        if (!TryComp<HandsComponent>(player, out var hands) ||
            !TryComp<GrapplingGunComponent>(hands.ActiveHandEntity, out var grappling))
        {
            return;
        }

        SetReeling(hands.ActiveHandEntity.Value, grappling, true, player.Value);
    }

    private void OnWeightlessMove(ref CanWeightlessMoveEvent ev)
    {
        if (ev.CanMove || !TryComp<JointComponent>(ev.Uid, out var jointComp) || !jointComp.GetJoints.ContainsKey(GrapplingJoint))
            return;

        ev.CanMove = true;
    }

    private void OnGunActivate(EntityUid uid, GrapplingGunComponent component, ActivateInWorldEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        _joints.RemoveJoint(Transform(uid).ParentUid, GrapplingJoint);
    }

    private void SetReeling(EntityUid uid, GrapplingGunComponent component, bool value, EntityUid? user)
    {
        if (component.Reeling == value)
            return;

        if (value)
        {
            _audio.PlayPredicted(component.ReelSound, uid, user);
        }
        else
        {
            component.Stream?.Stop();
        }

        component.Reeling = value;
        Dirty(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var xformQuery = GetEntityQuery<TransformComponent>();
        var query = EntityQueryEnumerator<GrapplingGunComponent>();

        while (query.MoveNext(out var uid, out var grappling))
        {
            if (!grappling.Reeling)
                continue;

            var xform = xformQuery.GetComponent(uid);

            if (!TryComp<JointComponent>(xform.ParentUid, out var jointComp) ||
                !jointComp.GetJoints.TryGetValue(GrapplingJoint, out var joint) ||
                joint is not DistanceJoint distance)
            {
                SetReeling(uid, grappling, false, null);
                continue;
            }

            distance.MaxLength = MathF.Max(distance.MinLength, distance.MaxLength - ReelRate * frameTime);
            Dirty(jointComp);

            if (distance.Length.Equals(distance.MinLength))
            {
                SetReeling(uid, grappling, false, null);
            }
        }
    }

    private void OnGunParentChange(EntityUid uid, GrapplingGunComponent component, ref EntParentChangedMessage args)
    {
        if (!Timing.IsFirstTimePredicted)
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
        if (!Timing.IsFirstTimePredicted)
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

    [Serializable, NetSerializable]
    public sealed class RequestGrapplingReelMessage : EntityEventArgs
    {

    }
}
