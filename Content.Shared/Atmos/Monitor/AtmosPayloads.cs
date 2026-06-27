using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class AtmosDeviceDataPayload : HandledNetworkPayload
{
    public bool Enabled;
    public bool Dirty;
    public bool IgnoreAlarms;
}
