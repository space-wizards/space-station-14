using Content.Shared.DeviceConfigurator.Systems;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceConfigurator.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(NetworkConfiguratorSystem))]
public sealed partial class NetworkConfiguratorComponent : Component
{
    // AAAAA ALL OF THESE FAA
    /// <summary>
    /// Determines whether the configurator is in linking mode or list mode
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LinkModeActive = true;

    /// <summary>
    /// The entity containing a <see cref="DeviceListComponent"/> this configurator is currently interacting with
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActiveDeviceList { get; set; }

    /// <summary>
    /// The entity containing a <see cref="DeviceLinkSourceComponent"/> or <see cref="DeviceLinkSinkComponent"/> this configurator is currently interacting with.<br/>
    /// If this is set the configurator is in linking mode.
    /// </summary>
    // TODO handle device deletion
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ActiveDeviceLink;

    /// <summary>
    /// The target device this configurator is currently linking with the <see cref="ActiveDeviceLink"/>
    /// </summary>
    // TODO handle device deletion
    [ViewVariables, AutoNetworkedField]
    public EntityUid? DeviceLinkTarget;

    /// <summary>
    /// The list of devices stored in the configurator
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<DeviceAddress, EntityUid> Devices = new();

    /// <summary>
    /// The list of localized devices stored in the configurator.
    /// Used for instant displaying in the Network Configurator UI
    /// instead of waiting for the server to send all names.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<DeviceAddress, (LocId? AddressPrefix, string Name)> NamedDevices = new();

    [DataField]
    public SoundSpecifier? SoundNoAccess = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField]
    public SoundSpecifier? SoundSwitchMode = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg");
}
