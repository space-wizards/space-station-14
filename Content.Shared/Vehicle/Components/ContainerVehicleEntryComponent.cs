using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Configures entry and exit interactions for a container vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ContainerVehicleEntryComponent : Component
{
    /// <summary>
    /// How long it takes to enter the container vehicle.
    /// </summary>
    [DataField]
    public float EntryDelay = 3;

    /// <summary>
    /// How long it takes to pull *another person*
    /// outside of the container vehicle. You can exit instantly yourself.
    /// </summary>
    [DataField]
    public float ExitDelay = 3;
}
