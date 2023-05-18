using System.Linq;
using Content.Server.Power.Components;

namespace Content.Server._FTL.Areas;

public sealed class AreaSystem : EntitySystem
{
    public List<EntityUid> GetAreasOnGrid(EntityUid gridUid)
    {
        var areas = new List<EntityUid>();

        foreach (var area in EntityQuery<ApcComponent>())
        {
            var xform = Transform(area.Owner);
            if (xform.GridUid == gridUid)
                areas.Add(area.Owner);
        }

        return areas;
    }
}
