using Content.Shared.Vehicle.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Occupies the operator's hands while they are operating a vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleHandBlockerComponent : Component
{
    /// <summary>
    /// The number of hands to occupy.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BlockedHands = 1;
}
