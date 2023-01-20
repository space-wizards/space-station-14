using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedNetworkConfiguratorSystem))]
public sealed class NetworkConfiguratorComponent : Component
{
    /// <summary>
    /// Determines whether the configurator is in linking mode or list mode
    /// </summary>
    [DataField("linkModeActive")]
    public bool LinkModeActive = false;

    /// <summary>
    /// The entity containing a <see cref="DeviceListComponent"/> this configurator is currently interacting with
    /// </summary>
    [DataField("activeDeviceList")]
    public EntityUid? ActiveDeviceList = null;

    /// <summary>
    /// The entity containing a <see cref="DeviceLinkSourceComponent"/> or <see cref="DeviceLinkSinkComponent"/> this configurator is currently interacting with.<br/>
    /// If this is set the configurator is in linking mode.
    /// </summary>
    [DataField("activeDeviceLink")]
    public EntityUid? ActiveDeviceLink = null;

    /// <summary>
    /// The target device this configurator is currently linking with the <see cref="ActiveDeviceLink"/>
    /// </summary>
    [DataField("deviceLinkTarget")]
    public EntityUid? DeviceLinkTarget = null;

    /// <summary>
    /// The list of devices stored in the configurator
    /// </summary>
    [DataField("devices")]
    public Dictionary<string, EntityUid> Devices = new();

    [DataField("soundNoAccess")]
    public SoundSpecifier SoundNoAccess = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField("soundLinkStart")]
    public SoundSpecifier SoundLinkStart = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorComponentState : ComponentState
{
    public readonly EntityUid? ActiveDeviceList;

    public NetworkConfiguratorComponentState(EntityUid? activeDeviceList)
    {
        ActiveDeviceList = activeDeviceList;
    }
}
