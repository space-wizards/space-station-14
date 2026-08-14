using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Enables and configures interaction-based entry and operator removal for a <see cref="ContainerVehicleComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
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
}
