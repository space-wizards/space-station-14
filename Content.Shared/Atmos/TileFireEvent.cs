namespace Content.Shared.Atmos;

/// <summary>
/// Event raised on an entity when it is standing on a tile that's on fire.
/// </summary>
/// <param name="Temperature">Current temperature of the hotspot this entity is exposed to.</param>
/// <param name="Volume">Current volume of the hotspot this entity is exposed to.
/// This is not the volume of the tile this entity is on.</param>
[ByRefEvent]
public readonly record struct TileFireEvent(float Temperature, float Volume);
