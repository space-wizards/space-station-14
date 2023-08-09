using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.Projectiles
{
    public abstract class SharedProjectileSystem : EntitySystem
    {
        public const string ProjectileFixture = "projectile";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
        }

        private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
        {
            if (component.IgnoreShooter && args.BodyB.Owner == component.Shooter)
            {
                args.Cancelled = true;
            }
        }

        public void SetShooter(ProjectileComponent component, EntityUid uid)
        {
            if (component.Shooter == uid) return;

            component.Shooter = uid;
            Dirty(component);
        }

        [NetSerializable, Serializable]
        public sealed class ProjectileComponentState : ComponentState
        {
            public ProjectileComponentState(EntityUid shooter, bool ignoreShooter)
            {
                Shooter = shooter;
                IgnoreShooter = ignoreShooter;
            }

            public EntityUid Shooter { get; }
            public bool IgnoreShooter { get; }
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
}
