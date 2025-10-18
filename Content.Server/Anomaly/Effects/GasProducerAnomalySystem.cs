using Content.Server.Atmos.EntitySystems;
using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Atmos;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using Robust.Shared.Map.Components;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="GasProducerAnomalyComponent"/> and the events from <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class GasProducerAnomalySystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasProducerAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnSupercritical(EntityUid uid, GasProducerAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        if (!component.ReleaseOnMaxSeverity)
            return;

        ReleaseGas(uid, component.ReleasedGas, component.SuperCriticalMoleAmount, component.spawnRadius, component.tileCount, component.tempChange);
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
            ReleaseGas(ent, comp.ReleasedGas, comp.PassiveMoleAmount * frameTime, comp.spawnRadius, comp.tileCount, comp.tempChange);
        }
    }

    private void ReleaseGas(EntityUid uid, Gas gas, float mols, float radius, int count, float temp)
    {
        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var localpos = xform.Coordinates.Position;
        var tilerefs = _map.GetLocalTilesIntersecting(
            xform.GridUid.Value,
            grid,
            new Box2(localpos + new Vector2(-radius, -radius), localpos + new Vector2(radius, radius)))
            .ToArray();

        if (tilerefs.Length == 0)
            return;

        var mixture = _atmosphere.GetTileMixture((uid, xform), true);
        if (mixture != null)
        {
            mixture.AdjustMoles(gas, mols);
            mixture.Temperature += temp;
        }

        if (count == 0)
            return;

        _random.Shuffle(tilerefs);
        var amountCounter = 0;
        foreach (var tileref in tilerefs)
        {
            var mix = _atmosphere.GetTileMixture(xform.GridUid, xform.MapUid, tileref.GridIndices, true);
            amountCounter++;
            if (mix is not { })
                continue;

            mix.AdjustMoles(gas, mols);
            mix.Temperature += temp;

            if (amountCounter >= count)
                return;
        }
    }
}

