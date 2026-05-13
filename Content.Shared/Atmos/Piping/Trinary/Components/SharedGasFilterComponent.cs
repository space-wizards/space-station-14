using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Trinary.Components
{
    [Serializable, NetSerializable]
    public enum GasFilterUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed partial class GasFilterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string FilterLabel { get; }
        public float TransferRate { get; }
        public bool Enabled { get; }
        public Gas? FilteredGas { get; }

        public GasFilterBoundUserInterfaceState(string filterLabel, float transferRate, bool enabled, Gas? filteredGas)
        {
            FilterLabel = filterLabel;
            TransferRate = transferRate;
            Enabled = enabled;
            FilteredGas = filteredGas;
        }
    }

    [Serializable, NetSerializable]
    public sealed partial class GasFilterToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public GasFilterToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public sealed partial class GasFilterChangeRateMessage : BoundUserInterfaceMessage
    {
        public float Rate { get; }

        public GasFilterChangeRateMessage(float rate)
        {
            Rate = rate;
        }
    }

    [Serializable, NetSerializable]
    public sealed partial class GasFilterSelectGasMessage(Gas? gas) : BoundUserInterfaceMessage
    {
        public readonly Gas? Gas = gas;
    }
}

