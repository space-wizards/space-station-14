using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components
{
    [NetworkedComponent]
    public abstract partial class SharedWelderComponent : Component { }

    [NetSerializable, Serializable]
    public sealed class WelderComponentState : ComponentState
    {
        public float FuelCapacity { get; }
        public float Fuel { get; }

        public WelderComponentState(float fuelCapacity, float fuel)
        {
            FuelCapacity = fuelCapacity;
            Fuel = fuel;
        }
    }
}
