using Content.Shared.CCVar;
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Content.Shared.Weather.Effects;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Random;

namespace Content.Server.Weather;

public sealed partial class WeatherSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityQuery<WeatherEffectsComponent> _weatherEffectsQuery;
    private EntityQuery<MapComponent> _mapCompQuery;
    private EntityQuery<RoofComponent> _roofQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private int _maxAffectedPerTick;
    private int _maxTilesScannedPerTick;

    /// <summary>
    /// Per-weather processing state for time-budgeted gathering and application.
    /// </summary>
    private readonly Dictionary<EntityUid, WeatherEffectProcessingState> _processingStates = new();

    /// <summary>
    /// Reusable buffer for entity lookups — avoids per-tile allocations.
    /// </summary>
    private readonly HashSet<EntityUid> _entityBuffer = new();

    private void InitEffects()
    {
        _weatherEffectsQuery = GetEntityQuery<WeatherEffectsComponent>();
        _mapCompQuery = GetEntityQuery<MapComponent>();
        _roofQuery = GetEntityQuery<RoofComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<WeatherEffectsComponent, ComponentInit>(OnWeatherEffectsInit);
        SubscribeLocalEvent<WeatherEffectsComponent, ComponentShutdown>(OnWeatherEffectsShutdown);

        Subs.CVar(_cfg, CCVars.WeatherMaxAffectedPerTick, val => _maxAffectedPerTick = val, true);
        Subs.CVar(_cfg, CCVars.WeatherMaxTilesScannedPerTick, val => _maxTilesScannedPerTick = val, true);
    }

    private void OnWeatherEffectsInit(Entity<WeatherEffectsComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextEffectTime = Timing.CurTime + ent.Comp.MaxEffectFrequency;
    }

    private void OnWeatherEffectsShutdown(Entity<WeatherEffectsComponent> ent, ref ComponentShutdown args)
    {
        _processingStates.Remove(ent.Owner);
    }

    private void UpdateEffects(float frameTime)
    {
        var query = EntityQueryEnumerator<WeatherStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var weather, out var status))
        {
            if (!_weatherEffectsQuery.TryGetComponent(uid, out var effects))
                continue;

            if (Timing.CurTime < effects.NextEffectTime)
                continue;

            var freq = _random.Next(effects.MinEffectFrequency, effects.MaxEffectFrequency);
            effects.NextEffectTime += freq;

            if (status.AppliedTo is not { } mapUid || !_mapCompQuery.TryGetComponent(mapUid, out var mapComp))
                continue;

            StartGatheringCycle(uid, mapUid, mapComp.MapId);
        }

        ProcessStates();
    }

    private void StartGatheringCycle(EntityUid weatherUid, EntityUid mapUid, MapId mapId)
    {
        var state = EnsureProcessingState(weatherUid);
        state.MapUid = mapUid;
        state.MapId = mapId;
        state.Phase = EffectProcessingPhase.Gathering;
        state.PendingEntities.Clear();
        state.ProcessedEntities.Clear();
        state.Grids.Clear();
        state.CurrentGridIndex = 0;
        state.TileEnumeratorValid = false;

        foreach (var grid in _mapManager.GetAllGrids(mapId))
        {
            state.Grids.Add(grid);
        }
    }

    private void ProcessStates()
    {
        foreach (var (weatherUid, state) in _processingStates)
        {
            if (state.Phase == EffectProcessingPhase.Idle)
                continue;

            if (!Exists(weatherUid))
            {
                state.Phase = EffectProcessingPhase.Idle;
                continue;
            }

            switch (state.Phase)
            {
                case EffectProcessingPhase.Gathering:
                    ProcessGathering(state);
                    break;

                case EffectProcessingPhase.Applying:
                    ProcessApplying(weatherUid, state);
                    break;
            }
        }
    }

    /// <summary>
    /// Tile-centric gathering: iterates grid tiles, checks weather exposure, collects affected entities.
    /// Budget-limited by <c>weather.max_tiles_scanned_per_tick</c>.
    /// </summary>
    private void ProcessGathering(WeatherEffectProcessingState state)
    {
        var tilesScanned = 0;

        while (state.CurrentGridIndex < state.Grids.Count)
        {
            var grid = state.Grids[state.CurrentGridIndex];
            var gridUid = grid.Owner;
            var gridComp = grid.Comp;

            _roofQuery.TryGetComponent(gridUid, out var roofComp);

            if (!state.TileEnumeratorValid)
            {
                state.CurrentTileEnumerator = _mapSystem.GetAllTilesEnumerator(gridUid, gridComp);
                state.TileEnumeratorValid = true;
            }

            while (state.CurrentTileEnumerator.MoveNext(out var tileRef))
            {
                tilesScanned++;

                if (!CanWeatherAffect((gridUid, gridComp, roofComp), tileRef.Value))
                {
                    if (tilesScanned >= _maxTilesScannedPerTick)
                        return;
                    continue;
                }

                // Find all entities on this weather-exposed tile.
                _entityBuffer.Clear();
                _lookup.GetLocalEntitiesIntersecting(gridUid, tileRef.Value.GridIndices, _entityBuffer,
                    gridComp: gridComp);

                foreach (var entUid in _entityBuffer)
                {
                    // Deduplicate: entities spanning multiple tiles are only queued once.
                    if (state.ProcessedEntities.Add(entUid))
                        state.PendingEntities.Enqueue(entUid);
                }

                if (tilesScanned >= _maxTilesScannedPerTick)
                    return;
            }

            state.CurrentGridIndex++;
            state.TileEnumeratorValid = false;
        }

        // All grids/tiles scanned — transition to applying.
        state.Phase = state.PendingEntities.Count > 0
            ? EffectProcessingPhase.Applying
            : EffectProcessingPhase.Idle;
    }

    /// <summary>
    /// Drains the pending entities queue, raising <see cref="WeatherEntityAffectedEvent"/> for each.
    /// Budget-limited by <c>weather.max_affected_per_tick</c>.
    /// </summary>
    private void ProcessApplying(EntityUid weatherUid, WeatherEffectProcessingState state)
    {
        var processed = 0;

        while (state.PendingEntities.TryDequeue(out var targetUid))
        {
            if (!_xformQuery.TryGetComponent(targetUid, out var xform) || xform.MapUid != state.MapUid)
                continue;

            var ev = new WeatherEntityAffectedEvent(targetUid);
            RaiseLocalEvent(weatherUid, ref ev);

            processed++;
            if (processed >= _maxAffectedPerTick)
                return;
        }

        state.Phase = EffectProcessingPhase.Idle;
    }

    private WeatherEffectProcessingState EnsureProcessingState(EntityUid weatherUid)
    {
        if (!_processingStates.TryGetValue(weatherUid, out var state))
        {
            state = new WeatherEffectProcessingState();
            _processingStates[weatherUid] = state;
        }

        return state;
    }
}

/// <summary>
/// Processing state for a single weather entity's effect cycle.
/// Supports pause/resume across ticks for budgeted processing.
/// </summary>
internal sealed class WeatherEffectProcessingState
{
    public EffectProcessingPhase Phase = EffectProcessingPhase.Idle;

    public EntityUid MapUid;
    public MapId MapId;

    // Gathering state — supports pause/resume across ticks.
    public List<Entity<MapGridComponent>> Grids = new();
    public int CurrentGridIndex;
    public GridTileEnumerator CurrentTileEnumerator;
    public bool TileEnumeratorValid;

    public readonly Queue<EntityUid> PendingEntities = new();
    public readonly HashSet<EntityUid> ProcessedEntities = new();
}

internal enum EffectProcessingPhase : byte
{
    Idle,
    Gathering,
    Applying,
}

/// <summary>
/// Raised on the weather entity for each exposed entity during the applying phase.
/// Subscribe on <see cref="WeatherEffectsComponent"/> to handle effect application.
/// </summary>
[ByRefEvent]
public record struct WeatherEntityAffectedEvent(EntityUid Target);
