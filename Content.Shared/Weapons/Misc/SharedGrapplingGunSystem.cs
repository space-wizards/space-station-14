using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Misc;

public abstract class SharedGrapplingGunSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public const string GrapplingJoint = "grappling";

    public const float ReelRate = 1.5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrapplingProjectileComponent, ProjectileEmbedEvent>(OnGrappleCollide);
        SubscribeLocalEvent<CanWeightlessMoveEvent>(OnWeightlessMove);
        SubscribeAllEvent<RequestGrapplingReelMessage>(OnGrapplingReel);

        SubscribeLocalEvent<GrapplingGunComponent, GunShotEvent>(OnGrapplingShot);
        SubscribeLocalEvent<GrapplingGunComponent, ActivateInWorldEvent>(OnGunActivate);
        SubscribeLocalEvent<GrapplingGunComponent, HandDeselectedEvent>(OnGrapplingDeselected);
    }

    private void OnGrapplingShot(EntityUid uid, GrapplingGunComponent component, ref GunShotEvent args)
    {
        foreach (var (shotUid, shoot) in args.Ammo)
        {
            if (!HasComp<GrapplingProjectileComponent>(shotUid))
                continue;

            // At least show the visuals.
            component.Projectile = shotUid.Value;
            Dirty(component);
            var visuals = EnsureComp<JointVisualsComponent>(shotUid.Value);
            visuals.Sprite =
                new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");
            visuals.OffsetA = new Vector2(0f, 0.5f);
        }
    }

    private void OnGrapplingDeselected(EntityUid uid, GrapplingGunComponent component, HandDeselectedEvent args)
    {
        SetReeling(uid, component, false, args.User);
    }

    private void OnGrapplingReel(RequestGrapplingReelMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (!TryComp<HandsComponent>(player, out var hands) ||
            !TryComp<GrapplingGunComponent>(hands.ActiveHandEntity, out var grappling))
        {
            return;
        }

        if (msg.Reeling &&
            (!TryComp<CombatModeComponent>(player, out var combatMode) ||
             !combatMode.IsInCombatMode))
        {
            return;
        }

        SetReeling(hands.ActiveHandEntity.Value, grappling, msg.Reeling, player.Value);
    }

    private void OnWeightlessMove(ref CanWeightlessMoveEvent ev)
    {
        if (ev.CanMove || !TryComp<JointRelayTargetComponent>(ev.Uid, out var relayComp))
            return;

        foreach (var relay in relayComp.Relayed)
        {
            if (TryComp<JointComponent>(relay, out var jointRelay) && jointRelay.GetJoints.ContainsKey(GrapplingJoint))
            {
                ev.CanMove = true;
                return;
            }
        }
    }

    private void OnGunActivate(EntityUid uid, GrapplingGunComponent component, ActivateInWorldEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        SetReeling(uid, component, false, args.User);

        if (!Deleted(component.Projectile))
        {
            if (_netManager.IsServer)
                QueueDel(component.Projectile.Value);

            component.Projectile = null;
            Dirty(component);
        }
    }

    private void SetReeling(EntityUid uid, GrapplingGunComponent component, bool value, EntityUid? user)
    {
        if (component.Reeling == value)
            return;

        if (value)
        {
            if (Timing.IsFirstTimePredicted)
                component.Stream = _audio.PlayPredicted(component.ReelSound, uid, user);
        }
        else
        {
            component.Stream?.Stop();
            component.Stream = null;
        }

        component.Reeling = value;
        Dirty(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        var query = EntityQueryEnumerator<GrapplingGunComponent>();

        while (query.MoveNext(out var uid, out var grappling))
        {
            if (!grappling.Reeling)
                continue;

            if (!TryComp<JointComponent>(uid, out var jointComp) ||
                !jointComp.GetJoints.TryGetValue(GrapplingJoint, out var joint) ||
                joint is not DistanceJoint distance)
            {
                SetReeling(uid, grappling, false, null);
                continue;
            }

            // TODO: This should be on engine.
            distance.MaxLength = MathF.Max(distance.MinLength, distance.MaxLength - ReelRate * frameTime);
            distance.Length = MathF.Min(distance.MaxLength, distance.Length);

            _physics.WakeBody(joint.BodyAUid);
            _physics.WakeBody(joint.BodyBUid);

            if (jointComp.Relay != null)
            {
                _physics.WakeBody(jointComp.Relay.Value);
            }

            Dirty(jointComp);

            if (distance.MaxLength.Equals(distance.MinLength))
            {
                SetReeling(uid, grappling, false, null);
            }
        }
    }

    private void OnGrappleCollide(EntityUid uid, GrapplingProjectileComponent component, ref ProjectileEmbedEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var jointComp = EnsureComp<JointComponent>(uid);
        _joints.RemoveJoint(uid, GrapplingJoint);
        var joint = _joints.CreateDistanceJoint(uid, args.Weapon, anchorA: new Vector2(0f, 0.5f), id: GrapplingJoint);
        joint.MaxLength = joint.Length + 0.2f;
        joint.Stiffness = 1f;
        joint.MinLength = 0.35f;
        Dirty(jointComp);
    }

    [Serializable, NetSerializable]
    protected sealed class RequestGrapplingReelMessage : EntityEventArgs
    {
        public bool Reeling;

        public RequestGrapplingReelMessage(bool reeling)
        {
            Reeling = reeling;
        }
    }
}
