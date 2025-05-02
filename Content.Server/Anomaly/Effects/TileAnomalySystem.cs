using System.Linq;
using System.Numerics;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class TileAnomalySystem : SharedTileAnomalySystem
{
    [Dependency] private readonly SharedAnomalySystem _anomaly = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalyShutdownEvent>(OnShutdown);
    }

    private void OnPulse(Entity<TileSpawnAnomalyComponent> component, ref AnomalyPulseEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnPulse)
                continue;

            SpawnTiles(component, entry, args.Stability, args.Severity, args.PowerModifier);
        }
    }

    private void OnSupercritical(Entity<TileSpawnAnomalyComponent> component, ref AnomalySupercriticalEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnSuperCritical)
                continue;

            SpawnTiles(component, entry, 1, 1, args.PowerModifier);
        }
    }

    private void OnShutdown(Entity<TileSpawnAnomalyComponent> component, ref AnomalyShutdownEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnShutdown || args.Supercritical)
                continue;

            SpawnTiles(component, entry, 1, 1, 1);
        }
    }

    private void OnStabilityChanged(Entity<TileSpawnAnomalyComponent> component, ref AnomalyStabilityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnStabilityChanged)
                continue;

            SpawnTiles(component, entry, args.Stability, args.Severity, 1);
        }
    }

    private void OnSeverityChanged(Entity<TileSpawnAnomalyComponent> component, ref AnomalySeverityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnSeverityChanged)
                continue;

            SpawnTiles(component, entry, args.Stability, args.Severity, 1);
        }
    }

    private void SpawnTiles(Entity<TileSpawnAnomalyComponent> anomaly, TileSpawnSettingsEntry entry, float stability, float severity, float powerMod)
    {
        var tiles = _anomaly.GetSpawningPoints(anomaly, stability, severity, entry.Settings, powerMod);
        if (tiles == null)
            return;

        foreach (var tileref in tiles)
        {
            var tile = (ContentTileDefinition) _tiledef[entry.Floor];
            _tile.ReplaceTile(tileref, tile);
        }
    }
}
