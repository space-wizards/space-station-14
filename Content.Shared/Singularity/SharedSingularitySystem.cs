using System;
using Content.Shared.Ghost;
using Content.Shared.Radiation;
using Content.Shared.Singularity.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Singularity
{
    public abstract class SharedSingularitySystem : EntitySystem
    {
        [Dependency] private readonly FixtureSystem _fixtures = default!;

        public const string DeleteFixture = "DeleteCircle";

        private float GetFalloff(int level)
        {
            return level switch
            {
                0 => 9999f,
                1 => 6.4f,
                2 => 7.0f,
                3 => 8.0f,
                4 => 10.0f,
                5 => 12.0f,
                6 => 12.0f,
                _ => -1.0f
            };
        }

        private float GetIntensity(int level)
        {
            return level switch
            {
                0 => 0.0f,
                1 => 2.7f,
                2 => 14.4f,
                3 => 47.2f,
                4 => 180.0f,
                5 => 600.0f,
                6 => 800.0f,
                _ => -1.0f
            };
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedSingularityComponent, PreventCollideEvent>(OnPreventCollide);
        }

        protected void OnPreventCollide(EntityUid uid, SharedSingularityComponent component, PreventCollideEvent args)
        {
            PreventCollide(uid, component, args);
        }

        protected virtual bool PreventCollide(EntityUid uid, SharedSingularityComponent component,
            PreventCollideEvent args)
        {
            var otherUid = args.BodyB.Owner;

            // For prediction reasons always want the client to ignore these.
            if (EntityManager.HasComponent<IMapGridComponent>(otherUid) ||
                EntityManager.HasComponent<SharedGhostComponent>(otherUid))
            {
                args.Cancel();
                return true;
            }

            // If we're above 4 then breach containment
            // otherwise, check if it's containment and just keep the collision
            if (EntityManager.HasComponent<SharedContainmentFieldComponent>(otherUid) ||
                EntityManager.HasComponent<SharedContainmentFieldGeneratorComponent>(otherUid))
            {
                if (component.Level > 4)
                {
                    args.Cancel();
                }

                return true;
            }

            return false;
        }

        public void ChangeSingularityLevel(SharedSingularityComponent singularity, int value)
        {
            if (value == singularity.Level)
            {
                return;
            }

            value = Math.Clamp(value, 0, 6);

            var physics = EntityManager.GetComponentOrNull<PhysicsComponent>(singularity.Owner);

            if (singularity.Level > 1 && value <= 1)
            {
                // Prevents it getting stuck (see SingularityController.MoveSingulo)
                if (physics != null)
                {
                    physics.LinearVelocity = Vector2.Zero;
                }
            }

            singularity.Level = value;

            if (EntityManager.TryGetComponent(singularity.Owner, out SharedRadiationPulseComponent? pulse))
            {
                pulse.RadsPerSecond = 10 * value;
            }

            if (EntityManager.TryGetComponent(singularity.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(SingularityVisuals.Level, value);
            }

            if (physics != null && _fixtures.GetFixtureOrNull(physics, DeleteFixture) is {Shape: PhysShapeCircle circle})
            {
                circle.Radius = value - 0.5f;
            }

            if (EntityManager.TryGetComponent(singularity.Owner, out SingularityDistortionComponent? distortion))
            {
                distortion.Falloff = GetFalloff(value);
                distortion.Intensity = GetIntensity(value);
            }

            singularity.Dirty();
        }
    }
}
