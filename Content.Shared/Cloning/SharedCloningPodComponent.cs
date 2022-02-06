using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Cloning
{
    [Virtual]
    public class SharedCloningPodComponent : Component
    {
        [Serializable, NetSerializable]
        public enum CloningPodVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum CloningPodStatus : byte
        {
            Idle,
            Cloning,
            Gore,
            NoMind
        }
    }
}
