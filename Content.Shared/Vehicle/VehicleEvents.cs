using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle;

/// <summary>
/// Raised on a container vehicle before starting an explicit entry interaction.
/// </summary>
/// <param name="entering">The entity attempting to enter the vehicle.</param>
public sealed class ContainerVehicleEntryAttemptEvent(EntityUid entering) : CancellableEntityEventArgs
{
    public EntityUid Entering { get; } = entering;
}

/// <summary>
/// Raised when an entity finishes attempting to enter a container vehicle.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ContainerVehicleEntryEvent : SimpleDoAfterEvent;
