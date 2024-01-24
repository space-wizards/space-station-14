using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Anomaly.Effects.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class EntityAnomalySystem : SharedEntityAnomalySystem
{
    [Dependency] private readonly SharedAnomalySystem _anomaly = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalyShutdownEvent>(OnShutdown);
    }

    private void OnPulse(Entity<EntitySpawnAnomalyComponent> component, ref AnomalyPulseEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnPulse)
                continue;

            SpawnEntities(component, entry, args.Stability, args.Severity);
        }
    }

    private void OnSupercritical(Entity<EntitySpawnAnomalyComponent> component, ref AnomalySupercriticalEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnSuperCritical)
                continue;

            SpawnEntities(component, entry, 1, 1);
        }
    }

    private void OnShutdown(Entity<EntitySpawnAnomalyComponent> component, ref AnomalyShutdownEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnShutdown || args.Supercritical)
                continue;

            SpawnEntities(component, entry, 1, 1);
        }
    }

    private void OnStabilityChanged(Entity<EntitySpawnAnomalyComponent> component, ref AnomalyStabilityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnStabilityChanged)
                continue;

            SpawnEntities(component, entry, args.Stability, args.Severity);
        }
    }

    private void OnSeverityChanged(Entity<EntitySpawnAnomalyComponent> component, ref AnomalySeverityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnSeverityChanged)
                continue;

            SpawnEntities(component, entry, args.Stability, args.Severity);
        }
    }

    private void SpawnEntities(Entity<EntitySpawnAnomalyComponent> anomaly, EntitySpawnSettingsEntry entry, float stability, float severity)
    {
        var xform = Transform(anomaly);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tiles = _anomaly.GetSpawningPoints(anomaly, stability, severity, entry.Settings);
        if (tiles == null)
            return;

        foreach (var tileref in tiles)
        {
            Spawn(_random.Pick(entry.Spawns), tileref.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
        }
    }
}
