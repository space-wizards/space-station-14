using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

[Serializable, NetSerializable]
public abstract partial class BaseAtmosDeviceData : IAtmosDeviceData
{
    [DataField]
    public bool Enabled { get; set; }

    [DataField]
    public bool Dirty { get; set; }

    [DataField]
    public bool IgnoreAlarms { get; set; }

    public abstract void RaisePayload(EntityUid uid, string address, SharedDeviceNetworkSystem deviceNetSys);
}
