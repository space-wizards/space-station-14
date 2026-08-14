using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle;

/// <summary>
/// Allows vehicle-specific systems to veto interaction-based entry.
/// </summary>
/// <remarks>
/// May be raised while checking interaction availability before an interaction has started.
/// Handlers should avoid side effects.
/// </remarks>
public sealed class ContainerVehicleEntryAttemptEvent(EntityUid entering) : CancellableEntityEventArgs
{
    public EntityUid Entering { get; } = entering;
}

/// <summary>
/// Raised when entry is denied because the entering entity cannot operate a vehicle without an operator.
/// </summary>
public sealed class ContainerVehicleEntryOperatorDeniedEvent(EntityUid entering)
{
    public EntityUid Entering { get; } = entering;
}

/// <summary>
/// Do-after event for interaction-based entry into a container vehicle.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ContainerVehicleEntryEvent : SimpleDoAfterEvent;

/// <summary>
/// Allows vehicle-specific systems to veto interaction-based operator removal.
/// </summary>
/// <remarks>
/// May be raised while checking interaction availability before an interaction has started.
/// Handlers should avoid side effects.
/// </remarks>
public sealed class ContainerVehicleExitAttemptEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
}

/// <summary>
/// Raised when delayed removal of a container vehicle's operator starts.
/// </summary>
public sealed class ContainerVehicleOperatorRemovalStartedEvent(EntityUid user)
{
    public EntityUid User { get; } = user;
}

/// <summary>
/// Do-after event for delayed removal of a container vehicle's operator.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ContainerVehicleExitEvent : SimpleDoAfterEvent;
