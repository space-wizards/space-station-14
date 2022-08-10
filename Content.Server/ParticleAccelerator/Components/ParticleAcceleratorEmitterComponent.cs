using Content.Shared.Singularity.Components;

namespace Content.Server.ParticleAccelerator.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(ParticleAcceleratorPartComponent))]
    public sealed class ParticleAcceleratorEmitterComponent : ParticleAcceleratorPartComponent
    {
        [DataField("emitterType")]
        public ParticleAcceleratorEmitterType Type = ParticleAcceleratorEmitterType.Center;

        public void Fire(ParticleAcceleratorPowerState strength)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            var projectile = entities.SpawnEntity("ParticlesProjectile", entities.GetComponent<TransformComponent>(Owner).Coordinates);

            if (!entities.TryGetComponent<ParticleProjectileComponent?>(projectile, out var particleProjectileComponent))
            {
                Logger.Error("ParticleAcceleratorEmitter tried firing particles, but they was spawned without a ParticleProjectileComponent");
                return;
            }
            particleProjectileComponent.Fire(strength, entities.GetComponent<TransformComponent>(Owner).WorldRotation, Owner);
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
