using Content.Shared.Station.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Station.Components;

/// <summary>
/// Entities with this component get flinged toward the station on MapInit.
/// </summary>
[RegisterComponent]
[Access(typeof(FlingTowardStationSystem)), NetworkedComponent]
public sealed partial class FlingTowardStationComponent : Component
{
    /// <summary>
    /// Speed at which you are thrown toward the station.
    /// </summary>
    [DataField]
    public float Speed = 10f;
}
