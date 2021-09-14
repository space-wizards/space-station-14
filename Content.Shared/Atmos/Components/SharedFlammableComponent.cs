using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components
{
    public class SharedFlammableComponent : Component
    {
        public override string Name => "Flammable";
    }

    [Serializable, NetSerializable]
    public enum FireVisuals
    {
        OnFire,
        FireStacks,
    }
}
