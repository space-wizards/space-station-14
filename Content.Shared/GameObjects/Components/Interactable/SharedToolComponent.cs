using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Interactable
{
    [Flags]
    public enum ToolQuality : byte
    {
        None =        0,
        Anchoring =      1,
        Prying =      1 << 1,
        Screwing =    1 << 2,
        Cutting =     1 << 3,
        Welding =     1 << 4,
        Multitool =   1 << 5,
    }

    public class SharedToolComponent : Component
    {
        public override string Name => "Tool";

        public virtual ToolQuality Qualities { get; set; }
    }

    [NetSerializable, Serializable]
    public class MultiToolComponentState : ComponentState
    {
        public ToolQuality Quality { get; }
        public override uint NetID => ContentNetIDs.MULTITOOLS;

        public MultiToolComponentState(ToolQuality quality)
        {
            Quality = quality;
        }
    }

    [NetSerializable, Serializable]
    public class WelderComponentState : ComponentState
    {
        public float FuelCapacity { get; }
        public float Fuel { get; }
        public bool Activated { get; }
        public ToolQuality Quality { get; }
        public override uint NetID => ContentNetIDs.WELDER;

        public WelderComponentState(float fuelCapacity, float fuel, bool activated)
        {
            FuelCapacity = fuelCapacity;
            Fuel = fuel;
            Activated = activated;
            Quality = ToolQuality.Welding;
        }
    }
}
