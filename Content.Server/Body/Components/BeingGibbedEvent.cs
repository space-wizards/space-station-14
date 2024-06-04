namespace Content.Server.Body.Components;

/// <summary>
/// Raised when a body gets gibbed, before it is deleted.
/// </summary>
[ByRefEvent]
public readonly record struct BeingGibbedEvent(HashSet<EntityUid> GibbedParts);
