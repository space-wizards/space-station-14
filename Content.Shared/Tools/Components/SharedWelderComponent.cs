using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Tools.Components
{
    [NetworkedComponent]
    public abstract partial class SharedWelderComponent : Component
    {
        public bool Lit { get; set; }
    }

    [NetSerializable, Serializable]
    public sealed class WelderComponentState : ComponentState
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

    [Serializable, NetSerializable]
    public enum WelderVisuals : byte
    {
        Lit
    }

    [Serializable, NetSerializable]
    public enum WelderLayers : byte
    {
        Base,
        Flame
    }
}
