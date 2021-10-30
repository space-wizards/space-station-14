using Content.Shared.Singularity.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.ParticleAccelerator.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(ParticleAcceleratorPartComponent))]
    public class ParticleAcceleratorEmitterComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorEmitter";
        [DataField("emitterType")]
        public ParticleAcceleratorEmitterType Type = ParticleAcceleratorEmitterType.Center;

        public void Fire(ParticleAcceleratorPowerState strength)
        {
            var projectile = Owner.EntityManager.SpawnEntity("ParticlesProjectile", Owner.Transform.Coordinates);

            if (!projectile.TryGetComponent<ParticleProjectileComponent>(out var particleProjectileComponent))
            {
                Logger.Error("ParticleAcceleratorEmitter tried firing particles, but they was spawned without a ParticleProjectileComponent");
                return;
            }
            particleProjectileComponent.Fire(strength, Owner.Transform.WorldRotation, Owner);
        }

        public override string ToString()
        {
            return base.ToString() + $" EmitterType:{Type}";
        }
    }

    public enum ParticleAcceleratorEmitterType
    {
        Left,
        Center,
        Right
    }
}
