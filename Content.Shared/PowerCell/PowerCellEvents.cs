namespace Content.Shared.PowerCell;

/// <summary>
/// Raised directed on an entity when its active power cell has no more charge to supply.
/// </summary>
[ByRefEvent]
public readonly record struct PowerCellSlotEmptyEvent;

/// <summary>
/// Raised directed at an entity with a power cell slot when a power cell is ejected/inserted.
/// </summary>
[ByRefEvent]
public record struct PowerCellChangedEvent(bool Ejected);
