using Content.Server.Anomaly.Effects.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Anomaly;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="PyroclasticAnomalyComponent"/> and the events from <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class PyroclasticAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
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
        mixture?.AdjustMoles(component.SupercriticalGas, component.SupercriticalMoleAmount);
        if (grid is { })
        {
            _atmosphere.HotspotExpose(grid.Value, indices, component.HotspotExposeTemperature, component.HotspotExposeVolume, true);
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
            var stackAmount = 1 + (int) (severity / 0.25f);
            _flammable.AdjustFireStacks(ent, stackAmount, flammable);
            _flammable.Ignite(ent, flammable);
        }
    }
}
