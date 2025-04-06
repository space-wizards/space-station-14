using Content.Server.Atmos.EntitySystems;
using Content.Shared.Matchstick.Components;
using Content.Shared.Matchstick.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Matchstick.EntitySystems;

public sealed class MatchstickSystem : SharedMatchstickSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    protected override void CreateMatchstickHotspot(Entity<MatchstickComponent> ent)
    {
        var xform = Transform(ent);

        if (xform.GridUid is not { } gridUid)
            return;

        var position = _transformSystem.GetGridOrMapTilePosition(ent, xform);

        _atmosphereSystem.HotspotExpose(gridUid, position, ent.Comp.BurnTemperature, 50, ent, true);
    }
}
