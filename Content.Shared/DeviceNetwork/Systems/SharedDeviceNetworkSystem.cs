using System.Diagnostics.CodeAnalysis;
using Content.Shared.Buffers;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
///     Entity system that handles everything device network related.
///     Device networking allows machines and devices to communicate with each other
///     while adhering to restrictions like range or being connected to the same power network.
/// </summary>
public abstract partial class SharedDeviceNetworkSystem : EntitySystem, IDevicePayloadRaiser
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;

    protected SharedRobustArrayPool<Device> DeviceArrayPool = default!;
    protected SharedRobustArrayPool<EntityUid?> EntityArrayPool = default!;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<DeviceNetworkComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExaminableAddress)
            args.PushText(Loc.GetString("device-address-examine-message", ("address", ent.Comp.Address)));
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<DeviceNetworkComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.AutoConnect)
            ConnectDevice(ent.AsNullable());
    }

    /// <summary>
    /// Automatically attempt to connect some devices when a map starts.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<DeviceNetworkComponent> ent, ref MapInitEvent args)
    {
        var device = ent.Comp;
        if (device.ReceiveFrequency == null
            && device.ReceiveFrequencyId != null
            && _protoMan.TryIndex(device.ReceiveFrequencyId, out var receive))
        {
            device.ReceiveFrequency = receive.Frequency;
        }

        if (device.TransmitFrequency == null
            && device.TransmitFrequencyId != null
            && _protoMan.TryIndex(device.TransmitFrequencyId, out var xmit))
        {
            device.TransmitFrequency = xmit.Frequency;
        }

        // Needed for example for tests, so when there's a device, there's also always a manager that can handle it.
        EnsureManager();

        if (ent.Comp.AutoConnect)
            ConnectDevice(ent.AsNullable());

        DirtyFields(ent.AsNullable(), null, nameof(DeviceNetworkComponent.ReceiveFrequency), nameof(DeviceNetworkComponent.TransmitFrequency));
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
