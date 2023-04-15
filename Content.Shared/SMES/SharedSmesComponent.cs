using Content.Shared.Power;
using Robust.Shared.Serialization;

namespace Content.Shared.SMES
{
    [Serializable, NetSerializable]
    public enum SmesVisuals
    {
        LastChargeState,
        LastChargeLevel,
    }

    [Serializable, NetSerializable]
    public enum SmesUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class SmesBoundInterfaceState : BoundUserInterfaceState
    {
        public readonly int Power;
        public readonly ExternalPowerState ExternalPower;
        public readonly float Charge;

        public SmesBoundInterfaceState(int power, ExternalPowerState externalPower, float charge)
        {
            Power = power;
            ExternalPower = externalPower;
            Charge = charge;
        }
    }
}
