using Content.Server.Atmos.EntitySystems;
using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="TempAffectingAnomalyComponent"/>
/// </summary>
public sealed class TempAffectingAnomalySystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TempAffectingAnomalyComponent, AnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var comp, out var anom, out var xform))
        {
            var grid = xform.GridUid;
            var map = xform.MapUid;
            var indices = _xform.GetGridTilePositionOrDefault((ent, xform));
            var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);

            if (mixture is { })
            {
                mixture.Temperature += comp.TempChangePerSecond * anom.Severity * frameTime;
            }

            if (grid != null && anom.Severity > comp.AnomalyHotSpotThreshold)
            {
                _atmosphere.HotspotExpose(grid.Value, indices, comp.HotspotExposeTemperature, comp.HotspotExposeVolume, ent, true);
            }
        }
    }
}
