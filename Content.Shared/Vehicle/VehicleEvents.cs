using Content.Shared.DoAfter;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle;

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
/// Event raised on the vehicle after an operator is set.
/// New operator can be null.
/// </summary>
[ByRefEvent]
public readonly record struct VehicleOperatorSetEvent(EntityUid? NewOperator, EntityUid? OldOperator);

/// <summary>
/// Event raised on a vehicle to check if it can run/move around.
/// </summary>
[ByRefEvent]
public record struct VehicleCanRunEvent(Entity<VehicleComponent> Vehicle, bool CanRun = true);

/// <summary>
/// Allows vehicle-specific systems to veto interaction-based entry.
/// </summary>
/// <remarks>
/// May be raised while checking interaction availability before an interaction has started.
/// Handlers should avoid side effects.
/// </remarks>
[ByRefEvent]
public record struct ContainerVehicleEntryAttemptEvent(EntityUid Entering, bool Cancelled = false);

/// <summary>
/// Allows vehicle-specific systems to veto interaction-based operator removal.
/// </summary>
/// <remarks>
/// May be raised while checking interaction availability before an interaction has started.
/// Handlers should avoid side effects.
/// </remarks>
[ByRefEvent]
public record struct ContainerVehicleExitAttemptEvent(EntityUid User, bool Cancelled = false);

/// <summary>
/// Do-after event for interaction-based entry into a container vehicle.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ContainerVehicleEntryEvent : SimpleDoAfterEvent;

/// <summary>
/// Do-after event for delayed removal of a container vehicle's operator.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ContainerVehicleExitEvent : SimpleDoAfterEvent;
