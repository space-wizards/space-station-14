using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components;

public sealed partial class GasVolumePumpDataPayload : NetworkPayloadBase<GasVolumePumpDataPayload>
{
    [DataField]
    public float LastMolesTransferred;
}

public sealed partial class GasVolumePumpSyncDataPayload : NetworkPayloadBase<GasVolumePumpSyncDataPayload>;

public sealed partial class GasVolumePumpSetDataPayload : NetworkPayloadBase<GasVolumePumpSetDataPayload>
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
