using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

[Serializable, NetSerializable]
public enum NetworkConfiguratorUiKey
{
    List,
    Configure,
    Link
}

[Serializable, NetSerializable]
public enum NetworkConfiguratorButtonKey
{
    Set,
    Add,
    Edit,
    Clear,
    Copy,
    Show
}

/// <summary>
/// Message sent when the remove button for one device on the list was pressed
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NetworkConfiguratorRemoveDeviceMessage : BoundUserInterfaceMessage
{
    public readonly string Address;

    public NetworkConfiguratorRemoveDeviceMessage(string address)
    {
        Address = address;
    }
}

/// <summary>
/// Message sent when the clear button was pressed
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NetworkConfiguratorClearDevicesMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class NetworkConfiguratorButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly NetworkConfiguratorButtonKey ButtonKey;

    public NetworkConfiguratorButtonPressedMessage(NetworkConfiguratorButtonKey buttonKey)
    {
        ButtonKey = buttonKey;
    }
}

[Serializable, NetSerializable]
public sealed partial class NetworkConfiguratorClearLinksMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed partial class NetworkConfiguratorToggleLinkMessage : BoundUserInterfaceMessage
{
    public readonly string Source;
    public readonly string Sink;

    public NetworkConfiguratorToggleLinkMessage(string source, string sink)
    {
        Source = source;
        Sink = sink;
    }
}

[Serializable, NetSerializable]
public sealed partial class NetworkConfiguratorLinksSaveMessage : BoundUserInterfaceMessage
{
    public readonly List<(string source, string sink)> Links;

    public NetworkConfiguratorLinksSaveMessage(List<(string source, string sink)> links)
    {
        Links = links;
    }
}

