using Content.Shared.Actions;

namespace Content.Shared.ParadoxClone;

/// <summary>
/// Fired when the paradox clone decides to spawn
/// </summary>
public sealed partial class ActionParadoxCloneMaterializeEvent : InstantActionEvent
{
}

/// <summary>
/// Fired when the paradox clone decides to look around for a spawning location
/// </summary>
public sealed partial class ActionParadoxCloneWanderEvent : InstantActionEvent
{
}
