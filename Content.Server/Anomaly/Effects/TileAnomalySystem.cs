using System.Linq;
using System.Numerics;
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
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {

        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
    }

    private void OnPulse(Entity<TileSpawnAnomalyComponent> component, ref AnomalyPulseEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.SpawnOnPulse)
                continue;

            SpawnTiles(component, entry, args.Stability, args.Severity);
        }
    }
    private void OnSupercritical(Entity<TileSpawnAnomalyComponent> component, ref AnomalySupercriticalEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.SpawnOnSuperCritical)
                continue;

            SpawnTiles(component, entry, 1, 1);
        }
    }

    private void OnStabilityChanged(Entity<TileSpawnAnomalyComponent> component, ref AnomalyStabilityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.SpawnOnStabilityChanged)
                continue;

            SpawnTiles(component, entry, args.Stability, args.Severity);
        }
    }

    private void OnSeverityChanged(Entity<TileSpawnAnomalyComponent> component, ref AnomalySeverityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.SpawnOnSeverityChanged)
                continue;

            SpawnTiles(component, entry, args.Stability, args.Severity);
        }
    }

    //TheShuEd:
    //I know it's a shitcode! I didn't write it! I just restructured the functions
    // To Do: make it reusable with EntityAnomalySustem
    private void SpawnTiles(Entity<TileSpawnAnomalyComponent> component, TileSpawnSettingsEntry entry, float stability, float severity)
    {
        var xform = Transform(component.Owner);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var amount = (int) (MathHelper.Lerp(entry.MinAmount, entry.MaxAmount, stability * severity) + 0.5f);

        var localpos = xform.Coordinates.Position;
        var tilerefs = grid.GetLocalTilesIntersecting(
            new Box2(localpos + new Vector2(-entry.MaxRange, -entry.MaxRange), localpos + new Vector2(entry.MaxRange, entry.MaxRange))).ToArray();

        if (tilerefs.Length == 0)
            return;

        _random.Shuffle(tilerefs);
        var amountCounter = 0;

        foreach (var tileref in tilerefs)
        {
            //cut outer circle
            if (MathF.Sqrt(MathF.Pow(tileref.X - xform.LocalPosition.X, 2) + MathF.Pow(tileref.Y - xform.LocalPosition.Y, 2)) > entry.MaxRange)
                continue;

            //cut inner circle
            if (MathF.Sqrt(MathF.Pow(tileref.X - xform.LocalPosition.X, 2) + MathF.Pow(tileref.Y - xform.LocalPosition.Y, 2)) < entry.MinRange)
                continue;

            amountCounter++;
            var tile = (ContentTileDefinition) _tiledef[entry.Floor];
            _tile.ReplaceTile(tileref, tile);

            if (amountCounter >= amount)
                return;
        }
    }
}
