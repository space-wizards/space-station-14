#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
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
