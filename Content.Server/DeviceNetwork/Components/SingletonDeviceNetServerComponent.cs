namespace Content.Server.DeviceNetwork.Components;

[RegisterComponent]
public sealed partial class SingletonDeviceNetServerComponent : Component
{
    /// <summary>
    ///     Whether the server can become the currently active server. The server being unavailable usually means that it isn't powered
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Available = true;


    /// <summary>
    ///     Whether the server is the currently active server for the station it's on
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Active = true;
}
