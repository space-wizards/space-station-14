using Content.Shared.DeviceNetwork.Systems;

namespace Content.Shared.Atmos.Monitor;

[ImplicitDataDefinitionForInheritors]
public partial interface IAtmosDeviceData
{
    bool Enabled { get; set; }

    bool Dirty { get; set; }

    bool IgnoreAlarms { get; set; }

    /// <summary>
    /// Creates a payload that contains this atmos device data and send it to the specified address.
    /// </summary>
    /// <param name="uid">Owner of this atmos data.</param>
    /// <param name="address">The target address.</param>
    /// <param name="deviceNetSys">The device network system.</param>
    void RaisePayload(EntityUid uid, string address, SharedDeviceNetworkSystem deviceNetSys);
}
