using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [NetSerializable, Serializable]
    public class WelderComponentState : ComponentState
    {
        public float FuelCapacity { get; }
        public float Fuel { get; }
        public bool Activated { get; }

        public WelderComponentState(float fuelCapacity, float fuel, bool activated) : base(ContentNetIDs.WELDER)
        {
            FuelCapacity = fuelCapacity;
            Fuel = fuel;
            Activated = activated;
        }
    }
}
