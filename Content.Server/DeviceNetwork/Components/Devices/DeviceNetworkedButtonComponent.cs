namespace Content.Server.DeviceNetwork.Components.Devices;

/// <summary>
///     This is used to indicate that the entity in question will send
///     an enum over the device network if it is interacted with.
/// </summary>
[RegisterComponent]
public sealed class DeviceNetworkedButtonComponent : Component
{
    /// <summary>
    ///     The enum to send when the entity is interacted with.
    ///     This is sent with the <see cref="DeviceNetworkConstants.CmdSetState"/>
    ///     command, with the same key used to store the enum.
    /// </summary>
    [DataField("sendOnPressed")]
    public Enum? SendOnPressed { get; }
}
