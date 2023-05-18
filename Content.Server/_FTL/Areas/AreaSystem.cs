using System.Linq;
using Content.Server.Power.Components;
using Content.Shared._FTL.Areas;

namespace Content.Server._FTL.Areas;

public sealed class AreaSystem : SharedAreasSystem
{
    public List<Area> GetAreasOnGrid(EntityUid? gridUid)
    {
        var areas = new List<Area>();

        foreach (var apc in EntityQuery<ApcComponent>())
        {
            var xform = Transform(apc.Owner);
            if (xform.GridUid != gridUid)
                continue;
            var meta = MetaData(apc.Owner);
            var area = new Area
            {
                Entity = apc.Owner,
                Name = meta.EntityName,
                Enabled = apc.MainBreakerEnabled
            };
            areas.Add(area);
        }

        return areas;
    }
}
