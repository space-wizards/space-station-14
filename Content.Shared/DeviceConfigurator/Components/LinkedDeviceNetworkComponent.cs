using Content.Shared.DeviceConfigurator.Systems;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceConfigurator.Components;

/// <summary>
/// A component added to entities with <see cref="DeviceNetworkComponent"/>
/// that are currently stored in some device configurators.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class LinkedDeviceNetworkComponent : Component
{
    /// <summary>
    /// A list of device-lists that this device is on.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(DeviceListSystem))]
    public HashSet<EntityUid> DeviceLists = new();

    /// <summary>
    /// A list of configurators that this device is on.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(NetworkConfiguratorSystem))]
    public HashSet<EntityUid> Configurators = new();
}
