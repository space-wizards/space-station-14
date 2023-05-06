using Content.Server.Atmos.EntitySystems;
using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="GasProducerAnomalyComponent"/> and the events from <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class GasProducerAnomalySystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasProducerAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnSupercritical(EntityUid uid, GasProducerAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        if (!component.ReleaseOnMaxSeverity)
            return;

        ReleaseGas(uid, component.ReleasedGas, component.SuperCriticalMoleAmount);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GasProducerAnomalyComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (!comp.ReleasePassively)
                continue;

            // Yes this is unused code since there are no anomalies that
            // release gas passively *yet*, but since I'm here I figured
            // I'd save someone some time and just add it for the future
            ReleaseGas(ent, comp.ReleasedGas, comp.PassiveMoleAmount * frameTime);
        }
    }

    private void ReleaseGas(EntityUid uid, Gas gas, float amount)
    {
        var xform = Transform(uid);
        var grid = xform.GridUid;
        var map = xform.MapUid;

        var indices = _xform.GetGridOrMapTilePosition(uid, xform);
        var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);

        if (mixture == null)
            return;

        mixture.AdjustMoles(gas, amount);

        if (grid is { })
        {
            foreach (var ind in _atmosphere.GetAdjacentTiles(grid.Value, indices))
            {
                var mix = _atmosphere.GetTileMixture(grid, map, ind, true);

                if (mix is not { })
                    continue;

                mix.AdjustMoles(gas, amount);
            }
        }
    }
}
