using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Weather;

public abstract class SharedWeatherSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roof = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private EntityQuery<BlockWeatherComponent> _blockQuery;
    private EntityQuery<WeatherStatusEffectComponent> _weatherQuery;

    public static readonly TimeSpan StartupTime = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan ShutdownTime = TimeSpan.FromSeconds(15);

    public override void Initialize()
    {
        base.Initialize();

        _blockQuery = GetEntityQuery<BlockWeatherComponent>();
        _weatherQuery = GetEntityQuery<WeatherStatusEffectComponent>();
    }

    public bool CanWeatherAffect(Entity<MapGridComponent> ent, TileRef tileRef, RoofComponent? roofComp = null)
    {
        if (tileRef.Tile.IsEmpty)
            return true;

        if (Resolve(ent, ref roofComp, false) && _roof.IsRooved((ent, ent.Comp, roofComp), tileRef.GridIndices))
            return false;

        var tileDef = (ContentTileDefinition) _tileDefManager[tileRef.Tile.TypeId];

        if (!tileDef.Weather)
            return false;


        var anchoredEntities = _mapSystem.GetAnchoredEntitiesEnumerator(ent, ent.Comp, tileRef.GridIndices);

        while (anchoredEntities.MoveNext(out var anchored))
        {
            if (_blockQuery.HasComponent(anchored.Value))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the current “strength” of the specified weather based on the duration of the status effect.
    /// </summary>
    public float GetWeatherPercent(Entity<StatusEffectComponent> ent)
    {
        var pauseTime = _metadata.GetPauseTime(ent);
        var elapsed = Timing.CurTime - (ent.Comp.StartEffectTime + pauseTime);
        var duration = ent.Comp.Duration;
        var remaining = duration - elapsed;

        if (remaining < ShutdownTime)
            return (float) (remaining / ShutdownTime);
        else if (elapsed < StartupTime)
            return (float) (elapsed / StartupTime);
        else
            return 1f;
    }

    /// <summary>
    /// Adds new weather to map. Dont remove other existed weathers.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <param name="duration">How long this weather should exist on map? If null - infinity duration</param>
    public void AddWeather(MapId mapId, EntProtoId weatherProto, TimeSpan? duration = null)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return;

        AddWeather(mapUid.Value, weatherProto, duration);
    }

    /// <summary>
    /// Adds new weather to map. Dont remove other existed weathers. If this type of weather already exists, it simply override its duration.
    /// </summary>
    /// <param name="mapUid">Target entity map</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <param name="duration">How long this weather should exist on map? If null - infinity duration</param>
    public void AddWeather(EntityUid mapUid, EntProtoId weatherProto, TimeSpan? duration = null)
    {
        _statusEffects.TrySetStatusEffectDuration(mapUid, weatherProto, out _ , duration);
    }

    /// <summary>
    /// Start slowly removing weather from map. Its should be gone after <see cref="ShutdownTime"/> seconds.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    public void RemoveWeather(MapId mapId, EntProtoId weatherProto)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return;

        RemoveWeather(mapUid.Value, weatherProto);
    }

    /// <summary>
    /// Start slowly removing weather from map. Its should be gone after <see cref="ShutdownTime"/> seconds.
    /// </summary>
    /// <param name="mapUid">Target entity map</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    public void RemoveWeather(EntityUid mapUid, EntProtoId weatherProto)
    {
        if (!_statusEffects.TryGetStatusEffect(mapUid, weatherProto, out var weatherEnt))
            return;

        if (!_weatherQuery.TryComp(weatherEnt, out _))
            return;

        _statusEffects.TrySetStatusEffectDuration(mapUid, weatherProto, ShutdownTime);
    }

    /// <summary>
    /// Removes all weather conditions except the specified one. If the specified weather does not exist on the map, it adds it.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <param name="duration">How long this weather should exist on map? If null - infinity duration</param>
    public void SetWeather(MapId mapId, EntProtoId? weatherProto, TimeSpan? duration = null)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return;

        if (_statusEffects.TryEffectsWithComp<WeatherStatusEffectComponent>(mapUid, out var effects))
        {
            foreach (var effect in effects)
            {
                var effectProto = MetaData(effect).EntityPrototype;
                if (effectProto is null)
                    continue;

                if (effectProto != weatherProto)
                {
                    RemoveWeather(mapUid.Value, effectProto); //Removing all others weathers
                    continue;
                }

                AddWeather(mapUid.Value, effectProto, duration); //Add specific weather, or override it duration
            }
        }
    }
}
