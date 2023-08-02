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
            SubscribeLocalEvent<EmbeddableProjectileComponent, StartCollideEvent>(OnEmbedCollide);
            SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate);
            SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnEmbedRemove);
        }

        private void OnEmbedActivate(EntityUid uid, EmbeddableProjectileComponent component, ActivateInWorldEvent args)
        {
            // Nuh uh
            if (component.RemovalTime == null)
                return;

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

        private void OnEmbedCollide(EntityUid uid, EmbeddableProjectileComponent component, ref StartCollideEvent args)
        {
            if (args.OurBody.LinearVelocity.Length() < component.MinimumSpeed)
                return;

            _physics.SetLinearVelocity(uid, Vector2.Zero, body: args.OurBody);
            _physics.SetBodyType(uid, BodyType.Static, body: args.OurBody);
            var xform = Transform(uid);
            _transform.SetParent(uid, xform, args.OtherEntity);

            if (component.Offset != Vector2.Zero)
            {
                _transform.SetLocalPosition(xform, xform.LocalPosition + xform.LocalRotation.RotateVec(component.Offset));
            }

            // Raise a specific event for projectiles.
            if (TryComp<ProjectileComponent>(uid, out var projectile))
            {
                var ev = new ProjectileEmbedEvent(projectile.Shooter, projectile.Weapon, args.OtherEntity);
                RaiseLocalEvent(uid, ref ev);
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
