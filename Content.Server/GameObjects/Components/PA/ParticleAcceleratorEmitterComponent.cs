using System;
using Newtonsoft.Json.Serialization;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorEmitterComponent : ParticleAcceleratorPartComponent
    {
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

        public override void Initialize()
        {
            base.Initialize();
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
                    Logger.Error("Emittercomponent without Type somehow got initialized");
                    break;
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
