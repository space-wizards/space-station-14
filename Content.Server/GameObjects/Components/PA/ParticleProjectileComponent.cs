using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Singularity;
using Content.Shared.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleProjectileComponent : Component, IStartCollide
    {
        public override string Name => "ParticleProjectile";
        private ParticleAcceleratorPowerState _state;
        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            if (otherFixture.Body.Owner.TryGetComponent<ServerSingularityComponent>(out var singularityComponent))
            {
                var multiplier = _state switch
                {
                    ParticleAcceleratorPowerState.Standby => 0,
                    ParticleAcceleratorPowerState.Level0 => 1,
                    ParticleAcceleratorPowerState.Level1 => 3,
                    ParticleAcceleratorPowerState.Level2 => 6,
                    ParticleAcceleratorPowerState.Level3 => 10,
                    _ => 0
                };
                singularityComponent.Energy += 10 * multiplier;
                Owner.Delete();
            }
            else if (otherFixture.Body.Owner.TryGetComponent<SingularityGeneratorComponent>(out var singularityGeneratorComponent))
            {
                singularityGeneratorComponent.Power += _state switch
                {
                    ParticleAcceleratorPowerState.Standby => 0,
                    ParticleAcceleratorPowerState.Level0 => 1,
                    ParticleAcceleratorPowerState.Level1 => 2,
                    ParticleAcceleratorPowerState.Level2 => 4,
                    ParticleAcceleratorPowerState.Level3 => 8,
                    _ => 0
                };
                Owner.Delete();
            }
        }

        public void Fire(ParticleAcceleratorPowerState state, Angle angle, IEntity firer)
        {
            _state = state;

            if (!Owner.TryGetComponent<PhysicsComponent>(out var physicsComponent))
            {
                Logger.Error("ParticleProjectile tried firing, but it was spawned without a CollidableComponent");
                return;
            }
            physicsComponent.BodyStatus = BodyStatus.InAir;

            if (!Owner.TryGetComponent<ProjectileComponent>(out var projectileComponent))
            {
                Logger.Error("ParticleProjectile tried firing, but it was spawned without a ProjectileComponent");
                return;
            }
            projectileComponent.IgnoreEntity(firer);

            var suffix = state switch
            {
                ParticleAcceleratorPowerState.Level0 => "0",
                ParticleAcceleratorPowerState.Level1 => "1",
                ParticleAcceleratorPowerState.Level2 => "2",
                ParticleAcceleratorPowerState.Level3 => "3",
                _ => "0"
            };

            if (!Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
            {
                Logger.Error("ParticleProjectile tried firing, but it was spawned without a SpriteComponent");
                return;
            }
            spriteComponent.LayerSetState(0, $"particle{suffix}");

            physicsComponent
                .LinearVelocity = angle.ToWorldVec() * 20f;

            Owner.Transform.LocalRotation = angle;
            Timer.Spawn(3000, () => Owner.Delete());
        }
    }
}
