namespace Content.Shared.PowerCell;

/// <summary>
/// Raised directed on an entity when its active power cell has no more charge to supply.
/// </summary>
[ByRefEvent]
public readonly record struct PowerCellSlotEmptyEvent;
