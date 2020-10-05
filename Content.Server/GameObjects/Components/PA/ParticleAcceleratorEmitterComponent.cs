using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Shared.GameObjects.Components;
using Content.Shared.Physics;
using Newtonsoft.Json.Serialization;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
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

            projectile.GetComponent<ParticleProjectileComponent>().Fire(ParticleAccelerator.Power, Owner.Transform.WorldRotation, Owner);
        }

        public override ParticleAcceleratorPartComponent[] GetNeighbours()
        {
            switch (Type)
            {
                case ParticleAcceleratorEmitterType.Left:
                    return new ParticleAcceleratorPartComponent[] {ParticleAccelerator.EmitterCenter};
                case ParticleAcceleratorEmitterType.Center:
                    return new ParticleAcceleratorPartComponent[] {ParticleAccelerator.EmitterLeft, ParticleAccelerator.EmitterRight, ParticleAccelerator.PowerBox};
                case ParticleAcceleratorEmitterType.Right:
                    return new ParticleAcceleratorPartComponent[] {ParticleAccelerator.EmitterCenter};
                default:
                    Logger.Error("Emittercomponent without Type somehow got initialized (Error at getNeighbours)");
                    break;
            }
            return new ParticleAcceleratorPartComponent[0];
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
