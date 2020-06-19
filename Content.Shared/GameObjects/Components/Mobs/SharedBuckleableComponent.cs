using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public class SharedBuckleableComponent : Component
    {
        public sealed override string Name => "Buckleable";

        [Serializable, NetSerializable]
        public enum BuckleVisuals
        {
            Buckled
        }
    }
}
