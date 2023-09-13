using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedNetworkConfiguratorSystem))]
public sealed partial class NetworkConfiguratorComponent : Component
{
    /// <summary>
    /// Determines whether the configurator is in linking mode or list mode
    /// </summary>
    [DataField("linkModeActive")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LinkModeActive = true;

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

    [DataField("useDelay")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(0.5);

    [DataField("lastUseAttempt", customTypeSerializer:typeof(TimeOffsetSerializer))]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastUseAttempt;

    [DataField("soundNoAccess")]
    public SoundSpecifier SoundNoAccess = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField("soundSwitchMode")]
    public SoundSpecifier SoundSwitchMode = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg");
}

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorComponentState : ComponentState
{
    public readonly NetEntity? ActiveDeviceList;
    public readonly bool LinkModeActive;

    public NetworkConfiguratorComponentState(NetEntity? activeDeviceList, bool linkModeActive)
    {
        ActiveDeviceList = activeDeviceList;
        LinkModeActive = linkModeActive;
    }
}
