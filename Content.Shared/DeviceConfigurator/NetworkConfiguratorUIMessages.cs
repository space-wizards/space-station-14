using Content.Shared.Actions;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceConfigurator;

public sealed partial class ClearAllOverlaysEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum NetworkConfiguratorVisuals
{
    Mode
}

[Serializable, NetSerializable]
public enum NetworkConfiguratorLayers
{
    ModeLight
}


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
public sealed class NetworkConfiguratorRemoveDeviceMessage(LocDeviceAddress address) : BoundUserInterfaceMessage
{
    public readonly LocDeviceAddress Address = address;
}

/// <summary>
/// Message sent when the clear button was pressed
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorListClearDevicesMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorSetMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorAddMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorClearMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorCopyMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorLinkClearMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorLinkToggleMessage(DeviceLink link) : BoundUserInterfaceMessage
{
    public readonly DeviceLink Link = link;
}

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorLinkSaveMessage(List<DeviceLink> links) : BoundUserInterfaceMessage
{
    public readonly List<DeviceLink> Links = links;
}
