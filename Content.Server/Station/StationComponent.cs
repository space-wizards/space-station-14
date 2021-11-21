using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using static Content.Server.Station.StationSystem;

namespace Content.Server.Station;

[RegisterComponent, Friend(typeof(StationSystem))]
public class StationComponent : Component
{
    public override string Name => "StationJobList";


    public StationId Station = StationId.Invalid;
}
