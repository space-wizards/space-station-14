using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public class SharedBuckleComponent : Component
    {
        public sealed override string Name => "Buckle";

        [Serializable, NetSerializable]
        public enum BuckleVisuals
        {
            Buckled
        }
    }
}
