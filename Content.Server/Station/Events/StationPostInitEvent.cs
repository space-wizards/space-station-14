namespace Content.Server.Station.Events;

/// <summary>
/// Raised directed on a station after it has been initialized.
/// </summary>
[ByRefEvent]
public readonly record struct StationPostInitEvent;
