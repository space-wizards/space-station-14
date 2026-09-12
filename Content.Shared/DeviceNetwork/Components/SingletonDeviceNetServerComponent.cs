using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SingletonDeviceNetServerSystem))]
public sealed partial class SingletonDeviceNetServerComponent : Component
{
    /// <summary>
    /// Whether the server can become the currently active server. The server being unavailable usually means that it isn't powered
    /// </summary>
    [DataField]
    public bool Available = true;

    /// <summary>
    /// Whether the server is the currently active server for the station it's on
    /// </summary>
    [DataField]
    public bool Active = true;
}
