using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components.Networks;

[RegisterComponent, NetworkedComponent]
public sealed partial class WiredNetworkManagerComponent : Component
{
    /// <summary>
    /// Grid ID that this network controls.
    /// </summary>
    [DataField]
    public EntityUid? GridId;
}
