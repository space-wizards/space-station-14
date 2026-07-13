using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Gravity;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Misc;

public abstract partial class SharedGrapplingGunSystem : VirtualController
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    /// <summary>
    /// Name of the joint between a grappling gun and its hook.
    /// </summary>
    public const string GrapplingJoint = "grappling";

    /// <summary>
    /// The "default" mass below which pulling strength is scaled, to prevent small entities from being yeeted too fast.
    /// </summary>
    public const float BaseWeightMass = 80f;

    public override void Initialize()
    {
        SubscribeLocalEvent<GrapplingProjectileComponent, JointRemovedEvent>(OnGrappleJointRemoved);
        SubscribeLocalEvent<GrapplingProjectileComponent, ComponentShutdown>(OnGrappleProjectileShutdown);
        SubscribeLocalEvent<GrapplingProjectileComponent, ProjectileEmbedEvent>(OnGrappleCollide);

        SubscribeLocalEvent<GrapplingGunComponent, GunShotEvent>(OnGrapplingShot);
        SubscribeLocalEvent<GrapplingGunComponent, ActivateInWorldEvent>(OnGunActivate);
        SubscribeLocalEvent<GrapplingGunComponent, HandDeselectedEvent>(OnGrapplingDeselected);

        SubscribeAllEvent<RequestGrapplingReelMessage>(OnGrapplingReel);
        SubscribeLocalEvent<CanWeightlessMoveEvent>(OnWeightlessMove);

        // TODO: After step trigger refactor, dropping a grappling gun should manually try and activate step triggers it's suppressing.

        SubscribeLocalEvent<GrapplingProjectileEmbedComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);

        UpdatesBefore.Add(typeof(SharedJointSystem)); // We want to run before joints are solved
        base.Initialize();
    }

    private void OnGrappleJointRemoved(EntityUid uid, GrapplingProjectileComponent component, JointRemovedEvent args)
    {
        if (_netManager.IsServer)
            QueueDel(uid);
    }

    private void OnGrappleProjectileShutdown(Entity<GrapplingProjectileComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<EmbeddableProjectileComponent>(ent, out var embedComp) || embedComp.EmbeddedIntoUid == null)
            return;

        if (!TryComp<GrapplingProjectileEmbedComponent>(embedComp.EmbeddedIntoUid, out var grapplingEmbedComp))
            return;

        grapplingEmbedComp.GrapplingProjectiles.Remove(ent);
    }

    private void OnGrappleCollide(EntityUid uid, GrapplingProjectileComponent component, ref ProjectileEmbedEvent args)
    {
        if (!Timing.IsFirstTimePredicted || !args.Weapon.HasValue || !TryComp<GrapplingGunComponent>(args.Weapon, out var grapple))
            return;

        var grapplePos = _transform.GetWorldPosition(args.Weapon.Value);
        var hookPos = _transform.GetWorldPosition(uid);
        if ((grapplePos - hookPos).Length() >= grapple.RopeMaxLength)
        {
            Ungrapple((args.Weapon.Value, grapple), true);
            return;
        }

        var embedComp = EnsureComp<GrapplingProjectileEmbedComponent>(args.Embedded);
        embedComp.GrapplingProjectiles.Add(uid);

        var joint = _joints.CreateDistanceJoint(uid, args.Weapon.Value, id: GrapplingJoint);
        joint.MaxLength = joint.Length + grapple.RopeMargin;
        joint.Stiffness = grapple.RopeStiffness;
        joint.MinLength = grapple.RopeMinLength; // Length of a tile to prevent pulling yourself into / through walls
        joint.Breakpoint = grapple.RopeBreakPoint;

        var jointCompGrapple = Comp<JointComponent>(args.Weapon.Value);

        // Since the grappling hook is offset from the grid, we need to update the local anchor so that the relay correctly uses the correct anchor for the grid.
        RefreshJointRelay((args.Embedded, embedComp));

        _joints.RefreshRelay(args.Weapon.Value, jointCompGrapple);
    }

    private void OnGrapplingShot(EntityUid uid, GrapplingGunComponent component, ref GunShotEvent args)
    {
        foreach (var (shotUid, _) in args.Ammo)
        {
            if (!HasComp<GrapplingProjectileComponent>(shotUid))
                continue;

            //todo: this doesn't actually support multigrapple
            // At least show the visuals.
            component.Projectile = shotUid.Value;
            DirtyField(uid, component, nameof(GrapplingGunComponent.Projectile));
            var visuals = EnsureComp<JointVisualsComponent>(shotUid.Value);
            visuals.Sprite = component.RopeSprite;
            visuals.Target = uid;
            Dirty(shotUid.Value, visuals);
        }

        TryComp<AppearanceComponent>(uid, out var appearance);
        _appearance.SetData(uid, SharedTetherGunSystem.TetherVisualsStatus.Key, false, appearance);
    }

    private void OnGunActivate(EntityUid uid, GrapplingGunComponent component, ActivateInWorldEvent args)
    {
        if (!Timing.IsFirstTimePredicted || args.Handled || !args.Complex)
            return;

        _audio.PlayPredicted(component.CycleSound, uid, args.User);
        Ungrapple((uid, component), false, args.User);

        args.Handled = true;
    }

    private void OnGrapplingDeselected(EntityUid uid, GrapplingGunComponent component, HandDeselectedEvent args)
    {
        SetReeling(uid, component, false, args.User);
    }

    private void OnGrapplingReel(RequestGrapplingReelMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!_hands.TryGetActiveItem(player, out var activeItem) ||
            !TryComp<GrapplingGunComponent>(activeItem, out var grappling))
        {
            return;
        }

        if (msg.Reeling &&
            (!TryComp<CombatModeComponent>(player, out var combatMode) ||
             !combatMode.IsInCombatMode))
        {
            return;
        }

        SetReeling(activeItem.Value, grappling, msg.Reeling, player);
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

    private void OnAnchorStateChanged(Entity<GrapplingProjectileEmbedComponent> entity, ref AnchorStateChangedEvent args)
    {
        foreach (var hook in entity.Comp.GrapplingProjectiles)
        {
            if (!TryComp<ProjectileComponent>(hook, out var projectileComp) || !TryComp<JointComponent>(hook, out var jointComp))
                continue;

            if (projectileComp.Weapon == null || !TryComp<GrapplingGunComponent>(projectileComp.Weapon, out var gunComp))
                continue;

            RefreshJointRelay(entity);
        }
    }

    /// <summary>
    /// Ungrapples the grappling hook, destroying the hook and severing the joint
    /// </summary>
    /// <param name="grapple">Entity for the grappling gun</param>
    /// <param name="isBreak">Whether to play the sound for the rope breaking</param>
    /// <param name="user">The user responsible for the ungrapple. Optional</param>
    public void Ungrapple(Entity<GrapplingGunComponent> grapple, bool isBreak, EntityUid? user = null)
    {
        if (!Timing.IsFirstTimePredicted || grapple.Comp.Projectile is not { } projectile)
            return;

        if (isBreak)
        {
            if (user != null)
                _audio.PlayPredicted(grapple.Comp.BreakSound, grapple.Owner, user);
            else if (_netManager.IsServer) // This feels... hacky.
                _audio.PlayPvs(grapple.Comp.BreakSound, grapple.Owner);
        }

        _appearance.SetData(grapple.Owner, SharedTetherGunSystem.TetherVisualsStatus.Key, true);

        if (_netManager.IsServer)
            QueueDel(projectile);

        SetReeling(grapple.Owner, grapple.Comp, false, user);
        grapple.Comp.Projectile = null;
        DirtyField(grapple.Owner, grapple.Comp, nameof(GrapplingGunComponent.Projectile));
        _gun.ChangeBasicEntityAmmoCount(grapple.Owner, 1);
    }

    private void SetReeling(EntityUid uid, GrapplingGunComponent component, bool value, EntityUid? user)
    {
        if (TryComp<JointComponent>(uid, out var jointComp) &&
            jointComp.GetJoints.TryGetValue(GrapplingJoint, out var joint) &&
            joint is DistanceJoint distance)
        {
            if (distance.MaxLength <= distance.MinLength + component.RopeFullyReeledMargin)
                value = false;
        }

        if (component.Reeling == value)
            return;

        if (value)
        {
            // We null-coalesce here because playing the sound again will cause it to become eternally stuck playing
            component.Stream ??= _audio.PlayPredicted(component.ReelSound, uid, user)?.Entity;
        }
        else if (!value && component.Stream.HasValue && Timing.IsFirstTimePredicted)
        {
            // The IsFirstTimePredicted check is important here because otherwise component.Stream will be set to null from an early cancellation if this isn't FirstTimePredicted
            component.Stream = _audio.Stop(component.Stream);
        }

        component.Reeling = value;

        DirtyField(uid, component, nameof(GrapplingGunComponent.Reeling));
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<GrapplingGunComponent, JointComponent>();

        while (query.MoveNext(out var uid, out var grappling, out var jointComp))
        {
            if (!jointComp.GetJoints.TryGetValue(GrapplingJoint, out var joint) ||
                joint is not DistanceJoint distance ||
                !TryComp<JointComponent>(joint.BodyAUid, out var hookJointComp))
            {
                if (_netManager.IsServer) // Client might not receive the joint due to PVS culling, so lets not spam them with 23895739 mispredicted ungrapples
                    Ungrapple((uid, grappling), true);
                continue;
            }

            // If the joint breaks, it gets disabled
            if (distance.Enabled == false)
            {
                Ungrapple((uid, grappling), true);
                continue;
            }

            var physicalGrapple = jointComp.Relay.HasValue ? jointComp.Relay.Value : joint.BodyBUid;
            var physicalHook = hookJointComp.Relay.HasValue ? hookJointComp.Relay.Value : joint.BodyAUid;

            // HACK: preventing both ends of the grappling hook from sleeping if neither are on the same grid, so that grid movement works as expected
            if (_transform.GetGrid(physicalHook) != _transform.GetGrid(physicalGrapple))
            {
                _physics.WakeBody(physicalHook);
                _physics.WakeBody(physicalGrapple);
            }
            // END OF HACK

            var bodyAWorldPos = _transform.GetWorldPosition(joint.BodyAUid);
            var bodyBWorldPos = _transform.GetWorldPosition(joint.BodyBUid);

            // The solver does not handle setting the rope's length, but we still need to work with a copy of it to prevent jank.
            var ropeLength = (bodyAWorldPos - bodyBWorldPos).Length();

            // Rope should just break, instantly, if the user is teleported past its max length
            if (ropeLength >= distance.MaxLength + grappling.RopeMargin)
            {
                Ungrapple((uid, grappling), true);
                continue;
            }

            if (!grappling.Reeling)
            {
                // Just in case.
                if (grappling.Stream.HasValue && Timing.IsFirstTimePredicted)
                    grappling.Stream = _audio.Stop(grappling.Stream);

                continue;
            }

            // TODO: Contracting DistanceJoints should be in engine
            if (distance.MaxLength >= ropeLength + grappling.RopeMargin)
            {
                distance.MaxLength = MathF.Max(distance.MinLength + grappling.RopeMargin, distance.MaxLength - grappling.ReelRate * frameTime);
                distance.MaxLength = MathF.Max(ropeLength + grappling.RopeMargin, distance.MaxLength);
                ropeLength = MathF.Min(distance.MaxLength, ropeLength);

                distance.Length = ropeLength;
            }


            if (ropeLength <= distance.MinLength + grappling.RopeFullyReeledMargin)
            {
                SetReeling(uid, grappling, false, null);
            }
            else if (ropeLength >= distance.MaxLength - grappling.RopeMargin)
            {
                // Checks if the entity is "tied" to the grid it is on via extra-gravity technology (e.g. magboots). If so, for the purposes of reeling it counts as if you're weighing the same as the grid.
                bool attachedToGrid;

                if (_transform.GetGrid(joint.BodyAUid) == _transform.GetGrid(joint.BodyBUid))
                {
                    attachedToGrid = false;
                }
                else
                {
                    if (jointComp.Relay != null)
                    {
                        _physics.WakeBody(jointComp.Relay.Value);
                        attachedToGrid = Transform(jointComp.Relay.Value).Anchored ||
                                         !_gravity.IsWeightless(jointComp.Relay.Value) &&
                                         !_gravity.IsWeightlessStatusFromGrid(jointComp.Relay.Value);
                    }
                    else
                    {
                        attachedToGrid = Transform(joint.BodyAUid).Anchored ||
                                         !_gravity.IsWeightless(joint.BodyAUid) &&
                                         !_gravity.IsWeightlessStatusFromGrid(joint.BodyAUid);
                    }
                }

                var targetDirection = (bodyAWorldPos - bodyBWorldPos).Normalized();

                var grapplerUidA = _container.TryGetOuterContainer(physicalHook, Transform(physicalHook), out var containerA) ? containerA.Owner : physicalHook;
                var grapplerOffsetA = _transform.GetRelativePosition(Transform(joint.BodyAUid), grapplerUidA);
                var grapplerBodyA = Comp<PhysicsComponent>(grapplerUidA);

                var grapplerUidB = _container.TryGetOuterContainer(physicalGrapple, Transform(physicalGrapple), out var containerB) ? containerB.Owner : physicalGrapple;
                if (attachedToGrid)
                    grapplerUidB = _transform.GetGrid(joint.BodyBUid) ?? grapplerUidB;
                var grapplerOffsetB = _transform.GetRelativePosition(Transform(joint.BodyBUid), grapplerUidB);
                var grapplerBodyB = Comp<PhysicsComponent>(grapplerUidB);

                // Handle edge-cases where the mass is zero (e.g. station anchor). Treat that as infinite weight.
                float massFactor;
                if (grapplerBodyA.Mass == 0f && grapplerBodyB.Mass != 0f)
                {
                    massFactor = 1f;
                }
                else if (grapplerBodyA.Mass != 0f && grapplerBodyB.Mass == 0f)
                {
                    massFactor = 0f;
                }
                else if (grapplerBodyA.Mass == 0f && grapplerBodyB.Mass == 0f)
                {
                    massFactor = 0.5f;
                }
                else
                {
                    // This assumes the bodies can move freely.
                    // It would be nice if any resisting force applied to the grid (e.g. if stuck on something and can't move closer) would transfer to the main entity.
                    massFactor = grapplerBodyA.Mass / (grapplerBodyA.Mass + grapplerBodyB.Mass);
                }

                // Note that this way of calculating the impulse does not take into account objects being stuck on things, e.g. a movable grapple point stuck behind a wall.
                // Ideally the contraction of the joint itself should take this into account, but alas, this works for now.

                var massFactorA = (1 - massFactor);
                if (grapplerBodyA.Mass < BaseWeightMass) // To prevent small things go zoomies
                    massFactorA *= grapplerBodyA.Mass / BaseWeightMass;

                _physics.ApplyLinearImpulse(grapplerUidA, targetDirection * massFactorA * grappling.ReelForce * frameTime * -1, grapplerOffsetA, body: grapplerBodyA);

                var massFactorB = massFactor;
                if (grapplerBodyB.Mass < BaseWeightMass) // To prevent small things go zoomies
                    massFactorB *= grapplerBodyB.Mass / BaseWeightMass;

                _physics.ApplyLinearImpulse(grapplerUidB, targetDirection * massFactorB * grappling.ReelForce * frameTime, grapplerOffsetB, body: grapplerBodyB);
            }

            Dirty(uid, jointComp);
        }
    }

    /// <summary>
    /// Checks whether the entity is hooked to something via grappling gun.
    /// </summary>
    /// <param name="entity">Entity to check.</param>
    /// <returns>True if hooked, false otherwise.</returns>
    public bool IsEntityHooked(Entity<JointRelayTargetComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        foreach (var uid in entity.Comp.Relayed)
        {
            if (HasComp<GrapplingGunComponent>(uid))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Updates the relay of any grappling hook to ensure it uses either the embedded entity, or the grid if the entity is anchored.
    /// </summary>
    private void RefreshJointRelay(Entity<GrapplingProjectileEmbedComponent> entity)
    {
        foreach (var hook in entity.Comp.GrapplingProjectiles)
        {
            if (!HasComp<GrapplingProjectileComponent>(hook) || !TryComp<JointComponent>(hook, out var jointComp))
                continue;

            if (!jointComp.GetJoints.TryGetValue(GrapplingJoint, out var joint))
                continue;

            if (Transform(entity).Anchored && _transform.GetGrid(entity.Owner) != null)
            {
                joint.LocalAnchorA = _transform.GetRelativePosition(Transform(hook), _transform.GetGrid(entity.Owner)!.Value);
                _joints.SetRelay(hook, _transform.GetGrid(entity.Owner));

            }
            else
            {
                joint.LocalAnchorA = Vector2.Zero;
                _joints.SetRelay(hook, entity.Owner, jointComp);
            }
        }
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
