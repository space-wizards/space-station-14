using Content.Server.Station.Components;

namespace Content.Server.Station.Events;

/// <summary>
/// Raised directed on a station after it has been initialized, as well as broadcast.
/// This gets raised after the entity has been map-initialized, and the station's centcomm map/entity (if any) has been
/// set up.
/// </summary>
[ByRefEvent]
public readonly record struct StationPostInitEvent(Entity<StationDataComponent> Station);
