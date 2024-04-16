using Content.Server.Atmos.EntitySystems;
using Content.Shared.Tools.Components;
using Robust.Server.GameObjects;

using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Tools;

public sealed class ToolSystem : SharedToolSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void TurnOn(Entity<WelderComponent> entity, EntityUid? user)
    {
        base.TurnOn(entity, user);
        var xform = Transform(entity);
        if (xform.GridUid is { } gridUid)
        {
            var position = _transformSystem.GetGridOrMapTilePosition(entity.Owner, xform);
            _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, entity.Owner, true);
        }
    }
}

