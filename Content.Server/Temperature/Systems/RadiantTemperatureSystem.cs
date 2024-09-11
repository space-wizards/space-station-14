using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Temperature.Systems;

public sealed class RadiantTemperatureSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadiantTemperatureComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var comp, out var xform))
        {
            var grid = xform.GridUid;
            var map = xform.MapUid;
            var indices = _xform.GetGridTilePositionOrDefault((ent, xform));
            var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);

            if (mixture is null)
                continue;

            // do not continue heating if air is hotter than goal temperature,
            // and the entity is a radiant HEAT source (positive temp changes)
            if (mixture.Temperature > comp.GoalTemperature && comp.TemperatureChangePerTick > 0)
                continue;

            // do not continue cooling if air is colder than goal temperature
            // and the entity is a radiant COOLING source (negative temp changes)
            if (mixture.Temperature < comp.GoalTemperature && comp.TemperatureChangePerTick < 0)
                continue;

            mixture.Temperature += comp.TemperatureChangePerTick * frameTime;
        }
    }
}
