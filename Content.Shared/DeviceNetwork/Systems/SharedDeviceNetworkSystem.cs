using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
///     Entity system that handles everything device network related.
///     Device networking allows machines and devices to communicate with each other
///     while adhering to restrictions like range or being connected to the same power network.
/// </summary>
public abstract partial class SharedDeviceNetworkSystem : EntitySystem, IDevicePayloadRaiser
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
    /// Sends the given <see cref="NetworkPayload"/> as a device network packet to the entity with the given address and frequency.
    /// Addresses are given to the <see cref="DeviceNetworkComponent"/> of an entity when connecting.
    /// </summary>
    /// <param name="ent">The sending entity.</param>
    /// <param name="address">
    /// The address of the entity that the packet gets sent to.
    /// If null, the message is broadcast to all devices on that frequency (except the sender)
    /// </param>
    /// <param name="frequency">The frequency to send on.</param>
    /// <param name="data">The data to be sent.</param>
    /// <param name="network">The network to send on.</param>
    /// <returns>Returns true when the packet was successfully enqueued.</returns>
    [PublicAPI]
    public virtual bool QueuePacket(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        INetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        return false;
    }

    /// <summary>
    /// Raises a device network packet to an entity. You should not be calling this unless you know what you're doing.
    /// </summary>
    public void RaisePayloadEvent<T>(EntityUid target, T payload, ref DeviceNetworkPacketData packet) where T : NetworkPayloadBase<T>
    {
        var ev = new DeviceNetworkPacketEvent<T>(
            packet.NetId,
            packet.Address,
            packet.Frequency,
            packet.SenderAddress,
            packet.Sender,
            payload);
        RaiseLocalEvent(target, ref ev);
    }
}

/// <summary>
/// Used to raise an <see cref="NetworkPayload"/> without losing the type of effect.
/// </summary>
public interface IDevicePayloadRaiser
{
    void RaisePayloadEvent<T>(EntityUid target, T payload, ref DeviceNetworkPacketData packet) where T : NetworkPayloadBase<T>;
}
