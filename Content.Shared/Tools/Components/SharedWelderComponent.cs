using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components
{
    [NetworkedComponent]
    public abstract class SharedWelderComponent : Component
    {
        public bool Lit { get; set; }
    }

    [NetSerializable, Serializable]
    public class WelderComponentState : ComponentState
    {
        public float FuelCapacity { get; }
        public float Fuel { get; }
        public bool Lit { get; }

        public WelderComponentState(float fuelCapacity, float fuel, bool lit)
        {
            FuelCapacity = fuelCapacity;
            Fuel = fuel;
            Lit = lit;
        }
    }
}
