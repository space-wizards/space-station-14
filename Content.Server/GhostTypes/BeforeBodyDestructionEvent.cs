namespace Content.Server.GhostTypes;

/// <summary>
/// Raised right before a body is deleted (gibbed or otherwise)
/// </summary>
[ByRefEvent]
public readonly record struct BeforeBodyDestructionEvent;

