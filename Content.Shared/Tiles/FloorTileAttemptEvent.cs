namespace Content.Shared.Tiles;

/// <summary>
/// Raised directed on a grid when attempting a floor tile placement.
/// </summary>
[ByRefEvent]
public record struct FloorTileAttemptEvent(Vector2i GridIndices, bool Cancelled = false);
