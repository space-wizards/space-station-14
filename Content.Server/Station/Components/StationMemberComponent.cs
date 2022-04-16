using Content.Server.Station.Systems;

namespace Content.Server.Station.Components;

[RegisterComponent, Friend(typeof(StationSystem))]
public sealed class StationMemberComponent : Component
{
    [ViewVariables]
    public EntityUid Station = EntityUid.Invalid;
}
