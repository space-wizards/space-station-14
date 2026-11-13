using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components.Networks;

/// <summary>
/// A helper component assigned to entities referenced by device network managers
/// in order to properly clear the networks when the owner is deleted.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeviceManagerRelatedComponent : Component
{
    [DataField]
    public List<EntityUid> Managers = new();
}
