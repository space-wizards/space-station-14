using Content.Server.Station.Systems;

namespace Content.Server.Station.Components;

/// <summary>
/// Controls spawning on the given station, tracking spawners present on it.
/// </summary>
[RegisterComponent, Access(typeof(StationSpawningSystem))]
public sealed partial class StationSpawningComponent : Component
{
}
