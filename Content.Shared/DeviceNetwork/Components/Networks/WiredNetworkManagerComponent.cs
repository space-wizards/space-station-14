using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components.Networks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WiredNetworkManagerComponent : Component
{
    /// <summary>
    /// Grid ID that this network controls.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? GridId;
}
