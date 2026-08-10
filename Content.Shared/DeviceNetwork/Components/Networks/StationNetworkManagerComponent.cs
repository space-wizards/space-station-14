using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components.Networks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationNetworkManagerComponent : Component
{
    /// <summary>
    /// Station entity ID that this network controls.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? StationId;
}
