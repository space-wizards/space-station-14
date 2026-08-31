using Robust.Shared.GameStates;

namespace Content.Shared.Station.Components;

/// <summary>
/// Entities with this component get flung toward the station on MapInit.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(FlingTowardStationSystem))]
public sealed partial class FlingTowardStationComponent : Component
{
    /// <summary>
    /// Speed at which you are thrown toward the station.
    /// </summary>
    [DataField]
    public float Speed = 10f;
}
