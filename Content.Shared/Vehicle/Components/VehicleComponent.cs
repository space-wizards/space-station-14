using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Vehicles are objects that have the behavior of moving when a player "operates" them.
/// The details of when the vehicle can operate and who the operator is are not defined here.
/// This simply contains the baseline behavior of the vehicle itself.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleComponent : Component
{
    /// <summary>
    /// The driver of this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Operator;

    /// <summary>
    /// Simple whitelist for determining who can operator this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? OperatorWhitelist;
}

[Flags]
public enum VehicleVisuals : byte
{
    HasOperator,
    CanRun,
}

/// <summary>
/// Event raised on operator when they begin to operate a vehicle
/// Values are configured before this event is raised.
/// </summary>
[ByRefEvent]
public readonly record struct OnVehicleEnteredEvent(Entity<VehicleComponent> Vehicle, EntityUid Operator);

/// <summary>
/// Event raised on operator when they stop operating a vehicle.
/// Values are configured after this event is raised.
/// </summary>
[ByRefEvent]
public readonly record struct OnVehicleExitedEvent(Entity<VehicleComponent> Vehicle, EntityUid Operator);

/// <summary>
/// Event raised on vehicle after an operator is set.
/// New operator can be null.
/// </summary>
[ByRefEvent]
public readonly record struct VehicleOperatorSetEvent(EntityUid? NewOperator);
