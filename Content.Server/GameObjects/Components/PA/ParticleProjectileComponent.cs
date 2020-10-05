using System;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Singularity;
using Content.Shared.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleProjectileComponent : Component, ICollideBehavior
    {
        public override string Name => "ParticleProjectile";
        private ParticleAcceleratorPowerState State;
        public void CollideWith(IEntity collidedWith)
        {
            if (collidedWith.TryGetComponent<SingularityComponent>(out var singularityComponent))
            {
                var multiplier = State switch
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
        }

        public void Fire(ParticleAcceleratorPowerState state, Angle angle, IEntity firer)
        {
            State = state;
            var physicsComponent = Owner.GetComponent<ICollidableComponent>();
            physicsComponent.Status = BodyStatus.InAir;

            var projectileComponent = Owner.GetComponent<ProjectileComponent>();
            projectileComponent.IgnoreEntity(firer);

            var suffix = state switch
            {
                ParticleAcceleratorPowerState.Standby => "0",
                ParticleAcceleratorPowerState.Level0 => "0",
                ParticleAcceleratorPowerState.Level1 => "1",
                ParticleAcceleratorPowerState.Level2 => "2",
                ParticleAcceleratorPowerState.Level3 => "3",
                _ => "0"
            };
            var spriteComponent = Owner.GetComponent<SpriteComponent>();
            spriteComponent.LayerSetState(0, "particle"+suffix);

            Owner
                .GetComponent<ICollidableComponent>()
                .EnsureController<BulletController>()
                .LinearVelocity = angle.ToVec() * 20f;

            Owner.Transform.LocalRotation = new Angle(angle + Angle.FromDegrees(180));
            Timer.Spawn(3000, () => Owner.Delete());

        }
    }
}
