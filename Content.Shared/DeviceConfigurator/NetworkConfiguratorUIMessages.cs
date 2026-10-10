using Content.Shared.Actions;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceConfigurator;

/// <summary>
/// Action event requesting that all device network overlays be cleared.
/// </summary>
public sealed partial class ClearAllOverlaysEvent : InstantActionEvent;

/// <summary>
/// Appearance data keys used by a network configurator.
/// </summary>
[Serializable, NetSerializable]
public enum NetworkConfiguratorVisuals
{
    Mode
}

/// <summary>
/// Appearance layers used by a network configurator.
/// </summary>
[Serializable, NetSerializable]
public enum NetworkConfiguratorLayers
{
    ModeLight
}

/// <summary>
/// Bound user interface keys used by a network configurator.
/// </summary>
[Serializable, NetSerializable]
public enum NetworkConfiguratorUiKey
{
    /// <summary>The device-list interface.</summary>
    List,

    /// <summary>The device configuration interface.</summary>
    Configure,

    /// <summary>The device-linking interface.</summary>
    Link
}

/// <summary>
/// Actions available through network configurator buttons.
/// </summary>
[Serializable, NetSerializable]
public enum NetworkConfiguratorButtonKey
{
    /// <summary>Set the selected device.</summary>
    Set,

    /// <summary>Add a device to the list.</summary>
    Add,

    /// <summary>Edit the selected device.</summary>
    Edit,

    /// <summary>Clear the current selection or value.</summary>
    Clear,

    /// <summary>Copy the selected device information.</summary>
    Copy,

    /// <summary>Show the selected device.</summary>
    Show
}

/// <summary>
/// Message sent when the remove button for a device on the list is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorRemoveDeviceMessage(DeviceAddress address) : BoundUserInterfaceMessage
{
    public readonly DeviceAddress Address = address;
}

/// <summary>
/// Message sent when the device list's clear button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorListClearDevicesMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message requesting the configurator to set the selected device.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorSetMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message requesting that the selected device be added to the list.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorAddMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message requesting that the current configuration be cleared.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorClearMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message requesting that the selected device information be copied.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorCopyMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message requesting that the current device-link selection be cleared.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorLinkClearMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message requesting that a device link be toggled in the configurator.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorLinkToggleMessage(DeviceLink link) : BoundUserInterfaceMessage
{
    public readonly DeviceLink Link = link;
}

/// <summary>
/// Message requesting that the configurator save the selected device links.
/// </summary>
[Serializable, NetSerializable]
public sealed class NetworkConfiguratorLinkSaveMessage(List<DeviceLink> links) : BoundUserInterfaceMessage
{
    public readonly List<DeviceLink> Links = links;
}
