using Content.Shared.DeviceNetwork.Systems;

namespace Content.Shared.Atmos.Monitor;

[ImplicitDataDefinitionForInheritors]
public partial interface IAtmosDeviceData
{
    bool Enabled { get; set; }

    bool Dirty { get; set; }

    bool IgnoreAlarms { get; set; }

    void RaisePayload(EntityUid uid, string address, SharedDeviceNetworkSystem deviceNetSys);
}
