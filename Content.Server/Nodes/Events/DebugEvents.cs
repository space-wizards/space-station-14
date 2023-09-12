namespace Content.Server.Nodes.Events.Debug;

/// <summary>
/// A global event raised when a player starts (or stops, or refilters) viewing node debug info.
/// Intended to be used by any systems who would like to contribute debug information.
/// </summary>
[ByRefEvent]
public readonly record struct NodeVisViewersChanged(bool ShouldSendVisState);
