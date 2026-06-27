using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components
{
    [Serializable, NetSerializable]
    public sealed partial class GasVolumePumpDataPayload : AtmosDeviceDataPayload
    {
        [DataField]
        public float LastMolesTransferred;
    }

    [Serializable, NetSerializable]
    public sealed partial class GasVolumePumpSyncDataPayload : HandledNetworkPayload;

    [Serializable, NetSerializable]
    public sealed partial class GasVolumePumpSetDataPayload : HandledNetworkPayload
    {
        [DataField]
        public GasVolumePumpDataPayload Payload;
    }

    [Serializable, NetSerializable]
    public enum GasVolumePumpUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class GasVolumePumpToggleStatusMessage : BoundUserInterfaceMessage
    {
        public bool Enabled { get; }

        public GasVolumePumpToggleStatusMessage(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GasVolumePumpChangeTransferRateMessage : BoundUserInterfaceMessage
    {
        public float TransferRate { get; }

        public GasVolumePumpChangeTransferRateMessage(float transferRate)
        {
            TransferRate = transferRate;
        }
    }
}
