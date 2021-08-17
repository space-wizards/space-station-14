using Content.Server.Projectiles.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;

namespace Content.Server.ParticleAccelerator.Components
{
    [RegisterComponent]
    public class ParticleProjectileComponent : Component
    {
        public override string Name => "ParticleProjectile";
        public ParticleAcceleratorPowerState State;

        public void Fire(ParticleAcceleratorPowerState state, Angle angle, IEntity firer)
        {
            State = state;

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

            if (!Owner.TryGetComponent<SinguloFoodComponent>(out var singuloFoodComponent))
            {
                Logger.Error("ParticleProjectile tried firing, but it was spawned without a SinguloFoodComponent");
                return;
            }
            var multiplier = State switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 1,
                ParticleAcceleratorPowerState.Level1 => 3,
                ParticleAcceleratorPowerState.Level2 => 6,
                ParticleAcceleratorPowerState.Level3 => 10,
                _ => 0
            };
            singuloFoodComponent.Energy = 10 * multiplier;

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
