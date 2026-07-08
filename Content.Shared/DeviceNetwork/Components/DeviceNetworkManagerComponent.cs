using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// A singleton entity that contains different caches and data related to Device Networks.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeviceNetworkManagerComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<int, DeviceNet> Networks = new(4);

    public readonly Queue<DeviceNetworkPacketData> QueueA = new();
    public readonly Queue<DeviceNetworkPacketData> QueueB = new();

    /// <summary>
    /// The queue being processed in the current tick
    /// </summary>
    [ViewVariables]
    public Queue<DeviceNetworkPacketData> ActiveQueue = null!;

    /// <summary>
    /// The queue that will be processed in the next tick
    /// </summary>
    [ViewVariables]
    public Queue<DeviceNetworkPacketData> NextQueue = null!;
}
