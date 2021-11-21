using System.Collections.Generic;
using Content.Server.Maps;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using static Content.Server.Roles.StationSystem;

namespace Content.Server.Roles
{
    [RegisterComponent, Friend(typeof(StationSystem))]
    public class StationComponent : Component
    {
        public override string Name => "StationJobList";


        public StationId Station = StationId.Invalid;
    }
}
