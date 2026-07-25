namespace Content.Shared.Polymorph;

// DS14-start
/// <summary>
/// Raised before an entity is polymorphed. Cancelling this event prevents all
/// polymorph side effects.
/// </summary>
[ByRefEvent]
public record struct PolymorphAttemptEvent(PolymorphConfiguration Configuration, bool Cancelled = false);
// DS14-end

/// <summary>
/// Raised locally on an entity when it polymorphs into another entity
/// </summary>
/// <param name="OldEntity">EntityUid of the entity before the polymorph</param>
/// <param name="NewEntity">EntityUid of the entity after the polymorph</param>
/// <param name="IsRevert">Whether this polymorph event was a revert back to the original entity</param>
[ByRefEvent]
public record struct PolymorphedEvent(EntityUid OldEntity, EntityUid NewEntity, bool IsRevert);
