using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract partial class SharedDeviceNetworkSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeviceNetworkComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<DeviceNetworkComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExaminableAddress)
            args.PushText(Loc.GetString("device-address-examine-message", ("address", ent.Comp.Address)));
    }

    /// <summary>
    /// Sends the given payload as a device network packet to the entity with the given address and frequency.
    /// Addresses are given to the DeviceNetworkComponent of an entity when connecting.
    /// </summary>
    /// <param name="ent">The sending entity</param>
    /// <param name="address">The address of the entity that the packet gets sent to. If null, the message is broadcast to all devices on that frequency (except the sender)</param>
    /// <param name="frequency">The frequency to send on</param>
    /// <param name="data">The data to be sent</param>
    /// <returns>Returns true when the packet was successfully enqueued.</returns>
    [PublicAPI]
    public virtual bool QueuePacket(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        NetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        return false;
    }

    [PublicAPI]
    public virtual bool QueuePacket(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        HandledNetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        return false;
    }

    [PublicAPI]
    public virtual bool QueuePacketParallel(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        HandledNetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        return false;
    }
}
