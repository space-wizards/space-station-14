using Content.Shared.Traitor.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Traitor;

/// <summary>
/// Event raised on the target entity when a hacking beacon is planted onto it.
/// </summary>
public sealed class StructureHackedEvent : EntityEventArgs;

/// <summary>
/// Event raised by another system when criteria is met when a hacking beacon is planted on an entity.
/// </summary>
public sealed class StructureHackCompletedEvent : EntityEventArgs;

/// <summary>
///     Event raised when the hijack beacon succeeds in hijacking the ATS.
/// </summary>
[ByRefEvent]
public record struct HijackBeaconSuccessEvent(int Fine)
{
    /// <summary>
    /// The total amount deducted from station accounts. Used in the announcement.
    /// </summary>
    public int Total = 0;
};

/// <summary>
/// Event raised on the target entity when a hacking beacon is unstuck from it.
/// </summary>
public sealed class BeaconRemovedEvent : EntityEventArgs;

/// <summary>
/// Event raised on the target entity when an attempt to stick a hacking beacon on an entity is made. Can be cancelled.
/// </summary>
public sealed class AttemptHackStructureEvent : CancellableEntityEventArgs;
