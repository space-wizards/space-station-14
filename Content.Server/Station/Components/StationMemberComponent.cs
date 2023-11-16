using Content.Server.Station.Systems;

namespace Content.Server.Station.Components;

/// <summary>
/// Indicates that a grid is a member of the given station.
/// </summary>
[RegisterComponent, Access(typeof(StationSystem))]
public sealed partial class StationMemberComponent : Component
{
    /// <summary>
    /// Station that this grid is a part of.
    /// </summary>
    [ViewVariables]
    public EntityUid Station = EntityUid.Invalid;
}
