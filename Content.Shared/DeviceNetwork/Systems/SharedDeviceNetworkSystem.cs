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
    /// Sends the given <see cref="NetworkPayload"/> as a device network packet to the entity with the given address and frequency.
    /// Addresses are given to the <see cref="DeviceNetworkComponent"/> of an entity when connecting.
    /// </summary>
    /// <remarks>
    /// This overload of the method raises a <see cref="DeviceNetworkPacketEvent"/> on the receiving entities,
    /// which is slower compared to the overload that accepts <see cref="HandledNetworkPayload"/>.
    /// Use this variation for cases when the packet must be handled by many different types of receivers in different places.
    /// </remarks>
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
        NetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        return false;
    }

    /// <summary>
    /// Sends the given <see cref="HandledNetworkPayload"/> as a device network packet to the entity with the given address and frequency.
    /// Addresses are given to the <see cref="DeviceNetworkComponent"/> of an entity when connecting.
    /// </summary>
    /// <remarks>
    /// This overload of the method uses <see cref="BeforeDevicePayloadSystem{T}"/> to cancel the sending
    /// and <see cref="DevicePayloadSystem{T}"/> to handle the payload.
    /// Remember that this is incompatible with systems that use <see cref="DeviceNetworkPacketEvent"/>,
    /// and systems that subscribe to <see cref="BeforePacketSentEvent"/> must also implement <see cref="BeforeDevicePayloadSystem{T}"/>
    /// to properly cancel the sending of the payload.
    /// </remarks>
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
        HandledNetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        return false;
    }
}
