using System.Collections.Concurrent;
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

    public readonly ConcurrentQueue<DeviceNetworkPacketEvent> QueueA = new();
    public readonly ConcurrentQueue<DeviceNetworkPacketEvent> QueueB = new();

    /// <summary>
    /// The queue being processed in the current tick
    /// </summary>
    [ViewVariables]
    public ConcurrentQueue<DeviceNetworkPacketEvent> ActiveQueue = null!;

    /// <summary>
    /// The queue that will be processed in the next tick
    /// </summary>
    [ViewVariables]
    public ConcurrentQueue<DeviceNetworkPacketEvent> NextQueue = null!;

    public readonly ConcurrentQueue<DeviceNetworkPacketHandledEvent> QueueC = new();
    public readonly ConcurrentQueue<DeviceNetworkPacketHandledEvent> QueueD = new();

    [ViewVariables]
    public ConcurrentQueue<DeviceNetworkPacketHandledEvent> HandledActiveQueue = null!;

    [ViewVariables]
    public ConcurrentQueue<DeviceNetworkPacketHandledEvent> HandledNextQueue = null!;

    public readonly ConcurrentQueue<DeviceNetworkPacketHandledEvent> QueueE = new();
    public readonly ConcurrentQueue<DeviceNetworkPacketHandledEvent> QueueF = new();

    [ViewVariables]
    public ConcurrentQueue<DeviceNetworkPacketHandledEvent> ParallelActiveQueue = null!;

    [ViewVariables]
    public ConcurrentQueue<DeviceNetworkPacketHandledEvent> ParallelNextQueue = null!;
}
