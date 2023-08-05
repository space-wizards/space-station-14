using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;
using Content.Shared.Sound.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles
{
    public abstract class SharedProjectileSystem : EntitySystem
    {
        public const string ProjectileFixture = "projectile";

        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
            SubscribeLocalEvent<EmbeddableProjectileComponent, ProjectileCollideEvent>(OnEmbedProjectileCollide);
            SubscribeLocalEvent<EmbeddableProjectileComponent, ThrowDoHitEvent>(OnEmbedThrowDoHit);
            SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate);
            SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnEmbedRemove);
        }

        private void OnEmbedActivate(EntityUid uid, EmbeddableProjectileComponent component, ActivateInWorldEvent args)
        {
            // Nuh uh
            if (component.RemovalTime == null)
                return;

            if (args.Handled || !TryComp<PhysicsComponent>(uid, out var physics) || physics.BodyType != BodyType.Static)
                return;

            args.Handled = true;

            _doAfter.TryStartDoAfter(new DoAfterArgs(args.User, component.RemovalTime.Value,
                new RemoveEmbeddedProjectileEvent(), eventTarget: uid, target: uid)
            {
                DistanceThreshold = SharedInteractionSystem.InteractionRange,
            });
        }

        private void OnEmbedRemove(EntityUid uid, EmbeddableProjectileComponent component, RemoveEmbeddedProjectileEvent args)
        {
            // Whacky prediction issues.
            if (args.Cancelled || _netManager.IsClient)
                return;

            if (component.DeleteOnRemove)
            {
                QueueDel(uid);
                return;
            }

            var xform = Transform(uid);
            TryComp<PhysicsComponent>(uid, out var physics);
            _physics.SetBodyType(uid, BodyType.Dynamic, body: physics, xform: xform);
            _transform.AttachToGridOrMap(uid, xform);

            // Land it just coz uhhh yeah
            var landEv = new LandEvent(args.User, true);
            RaiseLocalEvent(uid, ref landEv);
            _physics.WakeBody(uid, body: physics);
        }

        private void OnEmbedThrowDoHit(EntityUid uid, EmbeddableProjectileComponent component, ThrowDoHitEvent args)
        {
            Embed(uid, args.Target, component);
        }

        private void OnEmbedProjectileCollide(EntityUid uid, EmbeddableProjectileComponent component, ref ProjectileCollideEvent args)
        {
            Embed(uid, args.OtherEntity, component);

            // Raise a specific event for projectiles.
            if (TryComp<ProjectileComponent>(uid, out var projectile))
            {
                var ev = new ProjectileEmbedEvent(projectile.Shooter, projectile.Weapon, args.OtherEntity);
                RaiseLocalEvent(uid, ref ev);
            }
        }

        private void Embed(EntityUid uid, EntityUid target, EmbeddableProjectileComponent component)
        {
            TryComp<PhysicsComponent>(uid, out var physics);
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
            _physics.SetBodyType(uid, BodyType.Static, body: physics);
            var xform = Transform(uid);
            _transform.SetParent(uid, xform, target);

            if (component.Offset != Vector2.Zero)
            {
                _transform.SetLocalPosition(xform, xform.LocalPosition + xform.LocalRotation.RotateVec(component.Offset));
            }
        }

        private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
        {
            if (component.IgnoreShooter && args.OtherEntity == component.Shooter)
            {
                args.Cancelled = true;
            }
        }

        public void SetShooter(ProjectileComponent component, EntityUid uid)
        {
            if (component.Shooter == uid)
                return;

            component.Shooter = uid;
            Dirty(uid, component);
        }

        [Serializable, NetSerializable]
        private sealed class RemoveEmbeddedProjectileEvent : DoAfterEvent
        {
            public override DoAfterEvent Clone() => this;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ImpactEffectEvent : EntityEventArgs
    {
        public string Prototype;
        public EntityCoordinates Coordinates;

        public ImpactEffectEvent(string prototype, EntityCoordinates coordinates)
        {
            Prototype = prototype;
            Coordinates = coordinates;
        }
    }
}

/// <summary>
/// Raised when entity is just about to be hit with projectile but can reflect it
/// </summary>
[ByRefEvent]
public record struct ProjectileReflectAttemptEvent(EntityUid ProjUid, ProjectileComponent Component, bool Cancelled);
