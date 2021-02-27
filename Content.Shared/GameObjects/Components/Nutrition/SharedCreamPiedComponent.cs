#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Nutrition
{
    public class SharedCreamPiedComponent : Component
    {
        public override string Name => "CreamPied";
    }

    [Serializable, NetSerializable]
    public enum CreamPiedVisuals
    {
        Creamed,
    }
}
