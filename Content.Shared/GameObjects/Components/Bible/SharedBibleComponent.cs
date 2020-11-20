using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Bible
{
    public class SharedBibleComponent : Component
    {
        public override string Name => "Bible";
    }

    [Serializable, NetSerializable]
    public enum BibleVisuals
    {
        Style,
    }
}
