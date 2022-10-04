namespace Content.Server.DeviceNetwork.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class DeviceNetworkedButtonComponent : Component
{
    [DataField("sendOnPressed")]
    public Enum? SendOnPressed { get; }
}
