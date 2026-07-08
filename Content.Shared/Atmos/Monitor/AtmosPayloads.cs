using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

public interface IAtmosDeviceDataPayload : INetworkPayload
{
    bool Enabled { get; set; }
    bool Dirty { get; set; }
    bool IgnoreAlarms { get; set; }
}

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class AtmosDeviceDataPayload<T> : NetworkPayloadBase<T>, IAtmosDeviceDataPayload where T : NetworkPayloadBase<T>
{
    public bool Enabled { get; set; }
    public bool Dirty { get; set; }
    public bool IgnoreAlarms { get; set; }
}
