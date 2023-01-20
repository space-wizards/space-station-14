using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Interaction;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="PyroclasticAnomalyComponent"/> and the events from <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class PyroclasticAnomalySystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PyroclasticAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<PyroclasticAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnPulse(EntityUid uid, PyroclasticAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var xform = Transform(uid);
        var ignitionRadius = component.MaximumIgnitionRadius * args.Stabiltiy;
        IgniteNearby(xform.Coordinates, args.Severity, ignitionRadius);
    }

    private void OnSupercritical(EntityUid uid, PyroclasticAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        var grid = xform.GridUid;
        var map = xform.MapUid;

        var indices = _xform.GetGridOrMapTilePosition(uid, xform);
        var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);

        if (mixture == null)
            return;
        mixture.AdjustMoles(component.SupercriticalGas, component.SupercriticalMoleAmount);
        if (grid is { })
        {
            foreach (var ind in _atmosphere.GetAdjacentTiles(grid.Value, indices))
            {
                var mix = _atmosphere.GetTileMixture(grid, map, indices, true);
                if (mix is not { })
                    continue;

                mix.AdjustMoles(component.SupercriticalGas, component.SupercriticalMoleAmount);
                mix.Temperature += component.HotspotExposeTemperature;
                _atmosphere.HotspotExpose(grid.Value, indices, component.HotspotExposeTemperature, mix.Volume, true);
            }
        }
        IgniteNearby(xform.Coordinates, 1, component.MaximumIgnitionRadius * 2);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (pyro, anom, xform) in EntityQuery<PyroclasticAnomalyComponent, AnomalyComponent, TransformComponent>())
        {
            var ent = pyro.Owner;

            var grid = xform.GridUid;
            var map = xform.MapUid;
            var indices = _xform.GetGridOrMapTilePosition(ent, xform);
            var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);
            if (mixture is { })
            {
                mixture.Temperature += pyro.HeatPerSecond * anom.Severity * frameTime;
            }

            if (grid != null && anom.Severity > pyro.AnomalyHotspotThreshold)
            {
                _atmosphere.HotspotExpose(grid.Value, indices, pyro.HotspotExposeTemperature, pyro.HotspotExposeVolume, true);
            }
        }
    }

    public void IgniteNearby(EntityCoordinates coordinates, float severity, float radius)
    {
        foreach (var flammable in _lookup.GetComponentsInRange<FlammableComponent>(coordinates, radius))
        {
            var ent = flammable.Owner;
            if (!_interaction.InRangeUnobstructed(coordinates.ToMap(EntityManager), ent, -1))
                continue;

            var stackAmount = 1 + (int) (severity / 0.25f);
            _flammable.AdjustFireStacks(ent, stackAmount, flammable);
            _flammable.Ignite(ent, flammable);
        }
    }
}
