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
public sealed class AttemptHackStructureEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// Is this the first time we've hacked a repeatable structure?
    /// </summary>
    public bool Repeat = false;
};

/// <summary>
/// Raised in the hacking beacon update loop on the object it is attached to until StopUpdating is set to true.
/// </summary>
public sealed class HackUpdateEvent : EntityEventArgs
{
    /// <summary>
    /// When will this event next be called?
    /// </summary>
    public TimeSpan NextUpdate;

    /// <summary>
    /// The hacking beacon calling this event.
    /// </summary>
    public Entity<ActiveHackingBeaconComponent> Beacon;

    /// <summary>
    /// Should we raise <see cref="StructureHackCompletedEvent"/>? This will stop the entity from receiving further update events and will complete an objective with <see cref="SabotageConditionComponent"/> and requireConfirmation set to true.
    /// </summary>
    public bool CompleteHack = false;
};
