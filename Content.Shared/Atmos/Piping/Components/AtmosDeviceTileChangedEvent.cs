namespace Content.Shared.Atmos.Piping.Components;

/// <summary>
/// Raised directed on entities when the tile that they reside in has had their
/// associated TileAtmosphere changed significantly, i.e. a tile/<see cref="GasMixture"/> being added, removed,
/// or replaced. Important when atmos devices need to update any stored references to their tile's atmosphere.
/// </summary>
[ByRefEvent]
public readonly record struct AtmosDeviceTileChangedEvent;
