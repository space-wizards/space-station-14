using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Lighter
{
    public abstract class SharedLighterSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
        }
    }

    [Serializable, NetSerializable]
    public enum LighterVisuals : byte
    {
        Status
    }

    [Serializable, NetSerializable]
    public enum LighterStatus : byte
    {
        On,
        Off
    }
}

