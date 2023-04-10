namespace Content.Shared.Spawners;

/// <summary>
/// Raised directed on an entity when its timed despawn is over.
/// </summary>
[ByRefEvent]
public readonly record struct TimedDespawnEvent;
