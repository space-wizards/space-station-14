namespace Content.Server.GhostTypes;

/// <summary>
/// Raised right before a body is deleted (gibbed or otherwise)
/// small change so i can try pushing again
/// </summary>
[ByRefEvent]
public readonly record struct BeforeBodyDestructionEvent;

