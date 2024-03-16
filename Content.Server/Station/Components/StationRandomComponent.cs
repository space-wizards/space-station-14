using Content.Server.Station.Systems;

namespace Content.Server.Station.Components;

/// <summary>
/// Stores station parameters that can be randomized by the roundstart
/// </summary>
[RegisterComponent, Access(typeof(StationSystem))]
public sealed partial class StationRandomComponent : Component
{
    [DataField]
    public bool EnableStationOffset = true;

    [DataField]
    public float MaxStationOffset = 1000.0f;

    [DataField]
    public bool EnableStationRotation = true;
}
