using System;
using Newtonsoft.Json.Serialization;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.PA
{
    [RegisterComponent]
    public class ParticleAcceleratorEmitterComponent : ParticleAcceleratorPartComponent
    {
        public override string Name => "ParticleAcceleratorEmitter";
        public ParticleAcceleratorEmitterType Type;

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
