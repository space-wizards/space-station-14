using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle;

/// <summary>
/// Allows vehicle-specific systems to veto an explicit entry attempt.
/// </summary>
/// <param name="entering">The entity attempting to enter the vehicle.</param>
public sealed class ContainerVehicleEntryAttemptEvent(EntityUid entering) : CancellableEntityEventArgs
{
    public EntityUid Entering { get; } = entering;
}

/// <summary>
/// Raised when an entity cannot enter an unoccupied container vehicle because it cannot operate it.
/// </summary>
/// <param name="entering">The entity that was denied entry.</param>
public sealed class ContainerVehicleEntryOperatorDeniedEvent(EntityUid entering)
{
    public EntityUid Entering { get; } = entering;
}

/// <summary>
/// Raised when an entity finishes attempting to enter a container vehicle.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ContainerVehicleEntryEvent : SimpleDoAfterEvent;

/// <summary>
/// Allows vehicle-specific systems to veto an explicit exit attempt.
/// </summary>
/// <param name="user">The entity performing the exit interaction.</param>
public sealed class ContainerVehicleExitAttemptEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
}

/// <summary>
/// Raised when delayed removal of a container vehicle's operator begins.
/// </summary>
/// <param name="user">The entity removing the operator.</param>
public sealed class ContainerVehicleOperatorRemovalStartedEvent(EntityUid user)
{
    public EntityUid User { get; } = user;
}

/// <summary>
/// Raised when delayed removal of a container vehicle's operator finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ContainerVehicleExitEvent : SimpleDoAfterEvent;
