using Robust.Shared.GameObjects;

namespace Content.Shared.Cloning.Events;

/// <summary>
///    Raised before a mob is cloned. Cancel to prevent cloning.
///    This is raised on the original mob.
/// </summary>
[ByRefEvent]
public record struct CloningAttemptEvent(CloningSettingsPrototype Settings, bool Cancelled = false);

/// <summary>
///    Raised after a component was copied to a clone.
///    This is raised on the original mob.
/// </summary>
[ByRefEvent]
public record struct CloningComponentEvent(CloningSettingsPrototype Settings, EntityUid CloneUid, IComponent Component);

/// <summary>
///    Raised after a new mob was spawned when cloning a humanoid.
///    This is raised on the original mob.
/// </summary>
[ByRefEvent]
public record struct CloningEvent(CloningSettingsPrototype Settings, EntityUid CloneUid);

/// <summary>
///    Raised after a new item was spawned when cloning an item.
///    This is raised on the original item.
/// </summary>
[ByRefEvent]
public record struct CloningItemEvent(EntityUid CloneUid);
