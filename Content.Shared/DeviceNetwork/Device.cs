using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// Represents a device in a <see cref="DeviceNet"/>.
/// </summary>
/// <remarks>
/// This type is read-only. To change any parameters of the device, use <see cref="SharedDeviceNetworkSystem"/>'s API.
/// </remarks>
[DataRecord]
public readonly partial record struct Device(EntityUid Owner, uint? ReceiveFrequency, string Address, bool ReceiveAll)
{
    public Device(Entity<DeviceNetworkComponent> ent) : this(
        ent.Owner,
        ent.Comp.ReceiveFrequency,
        ent.Comp.Address,
        ent.Comp.ReceiveAll)
    {
        Owner = ent.Owner;
        ReceiveFrequency = ent.Comp.ReceiveFrequency;
        ReceiveAll = ent.Comp.ReceiveAll;
    }
}
