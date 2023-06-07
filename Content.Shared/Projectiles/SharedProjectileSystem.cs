using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles
{
    public abstract class SharedProjectileSystem : EntitySystem
    {
        public const string ProjectileFixture = "projectile";

        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
            SubscribeLocalEvent<EmbeddableProjectileComponent, StartCollideEvent>(OnEmbedCollide);
        }

        private void OnEmbedCollide(EntityUid uid, EmbeddableProjectileComponent component, ref StartCollideEvent args)
        {
            if (!TryComp<ProjectileComponent>(uid, out var projectile))
                return;

            _physics.SetLinearVelocity(uid, Vector2.Zero, body: args.OurBody);
            _physics.SetBodyType(uid, BodyType.Static, body: args.OurBody);
            _transform.SetParent(uid, args.OtherEntity);
            var ev = new ProjectileEmbedEvent(projectile.Shooter, projectile.Weapon, args.OtherEntity);
            RaiseLocalEvent(uid, ref ev);
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
            Dirty(component);
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
