using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    [ComponentReference(typeof(ParticleAcceleratorPartComponent))]
    public class ParticleAcceleratorEmitterComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorEmitter";
        public ParticleAcceleratorEmitterType Type;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref Type, "emitterType", ParticleAcceleratorEmitterType.Center);
        }

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
