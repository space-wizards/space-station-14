namespace Content.Shared.Roles;

/// <summary>
/// Raised directed on an entity to apply extra loadout data.
/// </summary>
[ByRefEvent]
public record struct ApplyLoadoutExtrasEvent(EntityUid Entity, Dictionary<string, string> Data);
