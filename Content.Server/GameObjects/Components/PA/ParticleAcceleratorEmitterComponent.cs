using System;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Shared.Physics;
using Newtonsoft.Json.Serialization;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorEmitterComponent : ParticleAcceleratorPartComponent
    {
        [Dependency] private IEntityManager _entityManager;

        public override string Name => "ParticleAcceleratorEmitter";
        public ParticleAcceleratorEmitterType Type;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var emitterType = "center";
            serializer.DataField(ref emitterType, "emitterType", "center");

            switch (emitterType)
            {
                case "left":
                    Type = ParticleAcceleratorEmitterType.Left;
                    break;
                case "center":
                    Type = ParticleAcceleratorEmitterType.Center;
                    break;
                case "right":
                    Type = ParticleAcceleratorEmitterType.Right;
                    break;
                default:
                    throw new PrototypeLoadException($"Invalid emittertype ({emitterType}) in ParticleAcceleratorEmitterComponent");
            }
        }

        protected override void RegisterAtParticleAccelerator()
        {
            switch (Type)
            {
                case ParticleAcceleratorEmitterType.Left:
                    ParticleAccelerator.EmitterLeft = this;
                    break;
                case ParticleAcceleratorEmitterType.Center:
                    ParticleAccelerator.EmitterCenter = this;
                    break;
                case ParticleAcceleratorEmitterType.Right:
                    ParticleAccelerator.EmitterRight = this;
                    break;
                default:
                    Logger.Error("Emittercomponent without Type somehow got initialized (Error at register)");
                    break;
            }
        }

        protected override void UnRegisterAtParticleAccelerator()
        {
            switch (Type)
            {
                case ParticleAcceleratorEmitterType.Left:
                    ParticleAccelerator.EmitterLeft = null;
                    break;
                case ParticleAcceleratorEmitterType.Center:
                    ParticleAccelerator.EmitterCenter = null;
                    break;
                case ParticleAcceleratorEmitterType.Right:
                    ParticleAccelerator.EmitterRight = null;
                    break;
                default:
                    Logger.Error("Emittercomponent without Type somehow got initialized (Error at unregister)");
                    break;
            }
        }

        public void Fire()
        {
            var projectile = _entityManager.SpawnEntity("ParticlesProjectile", Owner.Transform.Coordinates);

            var physicsComponent = projectile.GetComponent<ICollidableComponent>();
            physicsComponent.Status = BodyStatus.InAir;

            var projectileComponent = projectile.GetComponent<ProjectileComponent>();
            projectileComponent.IgnoreEntity(Owner);

            projectile
                .GetComponent<ICollidableComponent>()
                .EnsureController<BulletController>()
                .LinearVelocity = Owner.Transform.WorldRotation.ToVec() * 20f;

            projectile.Transform.LocalRotation = Owner.Transform.WorldRotation;
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
