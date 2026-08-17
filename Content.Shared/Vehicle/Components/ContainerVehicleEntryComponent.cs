using Content.Shared.Vehicle.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Enables and configures interaction-based entry and operator removal for a <see cref="ContainerVehicleComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(VehicleSystem))]
public sealed partial class ContainerVehicleEntryComponent : Component
{
    /// <summary>
    /// Delay before an entry interaction completes.
    /// </summary>
    [DataField]
    public float EntryDelay = 3;

    /// <summary>
    /// Delay before an operator removal interaction completes.
    /// </summary>
    [DataField]
    public float ExitDelay = 3;

    /// <summary>
    /// Popup shown when an entity cannot become the vehicle's operator.
    /// </summary>
    [DataField]
    public LocId EntryDeniedPopup = "container-vehicle-entry-denied";

    /// <summary>
    /// Popup shown when another entity starts removing the vehicle's operator.
    /// </summary>
    [DataField]
    public LocId OperatorRemovalPopup = "container-vehicle-operator-removal-started";
}
