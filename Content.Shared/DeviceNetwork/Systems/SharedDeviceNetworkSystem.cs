using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract class SharedDeviceNetworkSystem : EntitySystem
{
    /// <summary>
    /// Sends the given payload as a device network packet to the entity with the given address and frequency.
    /// Addresses are given to the DeviceNetworkComponent of an entity when connecting.
    /// </summary>
    /// <param name="uid">The EntityUid of the sending entity</param>
    /// <param name="address">The address of the entity that the packet gets sent to. If null, the message is broadcast to all devices on that frequency (except the sender)</param>
    /// <param name="frequency">The frequency to send on</param>
    /// <param name="data">The data to be sent</param>
    /// <returns>Returns true when the packet was successfully enqueued.</returns>
    public virtual bool QueuePacket(EntityUid uid,
        string? address,
        NetworkPayload data,
        uint? frequency = null,
        int? network = null,
        DeviceNetworkComponent? device = null)
    {
        return false;
    }
}
