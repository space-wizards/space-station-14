using Content.Shared.Station;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Station;

[RegisterComponent, Friend(typeof(StationSystem))]
public class StationComponent : Component
{
    [ViewVariables]
    public StationId Station = StationId.Invalid;
}
