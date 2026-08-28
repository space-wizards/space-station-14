using Content.Shared.Station.Systems;

namespace Content.Shared.Station.Components;

/// <summary>
/// Entities with this component get flinged toward the station on MapInit.
/// </summary>
[RegisterComponent]
[Access(typeof(FlingTowardStationSystem))]
public sealed partial class FlingTowardStationComponent : Component
{
    /// <summary>
    /// Speed at which you are thrown toward the station.
    /// </summary>
    [DataField]
    public float Speed = 10f;
}
