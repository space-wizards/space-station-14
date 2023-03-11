using Content.Shared.Projectiles;
using Robust.Shared.Map;
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
            var target = args.BodyB.Owner;
            if (component.IgnoreShooter && target == component.Shooter)
            {
                args.Cancelled = true;
                return;
            }
            var attemptEv = new PreventProjectileCollideEvent(uid, component, false);
            RaiseLocalEvent(target, ref attemptEv);
            args.Cancelled = attemptEv.Cancelled;
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

[ByRefEvent]
public record struct PreventProjectileCollideEvent(EntityUid ProjUid, ProjectileComponent ProjComp, bool Cancelled);
