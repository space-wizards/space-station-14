using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// A singleton entity that contains different caches and data related to Device Networks.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeviceNetworkManagerComponent : Component
{
    public readonly Dictionary<int, DeviceNet> Networks = new(4);

    public readonly Queue<DeviceNetworkPacketEvent> QueueA = new();
    public readonly Queue<DeviceNetworkPacketEvent> QueueB = new();

    /// <summary>
    /// The queue being processed in the current tick
    /// </summary>
    [ViewVariables]
    public Queue<DeviceNetworkPacketEvent> ActiveQueue = null!;

    /// <summary>
    /// The queue that will be processed in the next tick
    /// </summary>
    [ViewVariables]
    public Queue<DeviceNetworkPacketEvent> NextQueue = null!;

    public readonly Queue<DeviceNetworkPacketHandledEvent> QueueC = new();
    public readonly Queue<DeviceNetworkPacketHandledEvent> QueueD = new();

    [ViewVariables]
    public Queue<DeviceNetworkPacketHandledEvent> StaticActiveQueue = null!;

    [ViewVariables]
    public Queue<DeviceNetworkPacketHandledEvent> StaticNextQueue = null!;
}
