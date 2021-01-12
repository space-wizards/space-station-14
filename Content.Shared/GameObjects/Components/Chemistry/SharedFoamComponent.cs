using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public class SharedFoamComponent : Component
    {
        public override string Name => "Foam";
    }

    [Serializable, NetSerializable]
    public enum FoamVisuals
    {
        State,
        Color
    }
}
