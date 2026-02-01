using System.Diagnostics.CodeAnalysis;
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

    public bool CanWeatherAffect(Entity<MapGridComponent?, RoofComponent?> ent, TileRef tileRef)
    {
        if (tileRef.Tile.IsEmpty)
            return true;

        if (!Resolve(ent, ref ent.Comp1))
            return false;

        if (Resolve(ent, ref ent.Comp2, false) && _roof.IsRooved((ent, ent.Comp1, ent.Comp2), tileRef.GridIndices))
            return false;

        var tileDef = (ContentTileDefinition) _tileDefManager[tileRef.Tile.TypeId];

        if (!tileDef.Weather)
            return false;

        var anchoredEntities = _mapSystem.GetAnchoredEntitiesEnumerator(ent, ent.Comp1, tileRef.GridIndices);

        while (anchoredEntities.MoveNext(out var anchored))
        {
            if (_blockQuery.HasComponent(anchored.Value))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the current “strength” of the specified weather based on the duration of the status effect.
    /// Between 0 and 1.
    /// </summary>
    public float GetWeatherPercent(Entity<StatusEffectComponent> ent)
    {
        var elapsed = Timing.CurTime - ent.Comp.StartEffectTime;
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
    /// Attempts to add a new weather status effect to the specified map.
    /// Does not remove or replace any other existing weather effects on the map.
    /// If the specified weather effect already exists, its duration will be overridden.
    /// </summary>
    /// <param name="mapId">The <see cref="MapId"/> of the target map to apply the weather effect to.</param>
    /// <param name="weatherProto">The prototype ID (<see cref="EntProtoId"/>) of the weather status effect to add.</param>
    /// <param name="weatherEnt">When this method returns, contains the <see cref="EntityUid"/> of the weather entity if the operation succeeded; otherwise, <c>null</c>.</param>
    /// <param name="duration">Optional. The duration for which the weather should exist on the map. If <c>null</c>, the weather will persist indefinitely.</param>
    /// <returns><c>true</c> if the weather was successfully added or updated; otherwise, <c>false</c>.</returns>
    public bool TryAddWeather(MapId mapId, EntProtoId weatherProto, [NotNullWhen(true)] out EntityUid? weatherEnt, TimeSpan? duration = null)
    {
        weatherEnt = null;

        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return false;

        return TryAddWeather(mapUid.Value, weatherProto, out weatherEnt, duration);
    }

    /// <summary>
    /// Adds a new weather to a map. Does not remove other existing weathers. If this type of weather already exists, it simply overrides its duration.
    /// </summary>
    /// <param name="mapUid">Target map entity</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <param name="weatherEnt">When this method returns, contains the <see cref="EntityUid"/> of the weather entity if the operation succeeded; otherwise, <c>null</c>.</param>
    /// <param name="duration">How long this weather should exist on the map? If null - infinite duration</param>
    public bool TryAddWeather(EntityUid mapUid, EntProtoId weatherProto, [NotNullWhen(true)] out EntityUid? weatherEnt, TimeSpan? duration = null)
    {
        return _statusEffects.TrySetStatusEffectDuration(mapUid, weatherProto, out weatherEnt, duration);
    }

    /// <summary>
    /// Checks if a specific weather exists on the given map.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <returns>True if the weather exists, otherwise false</returns>
    public bool HasWeather(MapId mapId, EntProtoId weatherProto)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return false;

        return _statusEffects.TryGetStatusEffect(mapUid.Value, weatherProto, out _);
    }

    /// <summary>
    /// Slowly remove weather from a map. It should be gone after <see cref="ShutdownTime"/> seconds.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    public bool TryRemoveWeather(MapId mapId, EntProtoId weatherProto)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return false;

        return TryRemoveWeather(mapUid.Value, weatherProto);
    }

    /// <summary>
    /// Slowly remove weather from map. It should be gone after <see cref="ShutdownTime"/> seconds.
    /// </summary>
    /// <param name="mapUid">Target entity map</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    public bool TryRemoveWeather(EntityUid mapUid, EntProtoId weatherProto)
    {
        if (!_statusEffects.TryGetStatusEffect(mapUid, weatherProto, out var weatherEnt))
            return false;

        if (!_weatherQuery.HasComp(weatherEnt))
            return false;

        return _statusEffects.TrySetStatusEffectDuration(mapUid, weatherProto, ShutdownTime);
    }

    /// <summary>
    /// Removes all weather conditions except the specified one. If the specified weather does not exist on the map, it adds it.
    /// Returns true if the specified weather is present or was added, false otherwise.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <param name="weatherEnt">When this method returns, contains the <see cref="EntityUid"/> of the weather entity if the operation succeeded; otherwise, <c>null</c>.</param>
    /// <param name="duration">How long this weather should exist on map? If null - infinite duration</param>
    /// <returns><c>true</c> if the specified weather is present or was added; otherwise, <c>false</c>.</returns>
    public bool TrySetWeather(MapId mapId, EntProtoId? weatherProto, out EntityUid? weatherEnt, TimeSpan? duration = null)
    {
        weatherEnt = null;
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return false;

        // Remove all other weather effects except the specified one
        if (_statusEffects.TryEffectsWithComp<WeatherStatusEffectComponent>(mapUid, out var effects))
        {
            foreach (var effect in effects)
            {
                var effectProto = Prototype(effect);
                if (effectProto is null)
                    continue;

                if (effectProto != weatherProto)
                {
                    TryRemoveWeather(mapUid.Value, effectProto);
                }
                else
                {
                    weatherEnt = effect;
                }
            }
        }

        // If weatherProto is null, we just removed all weather and return true
        if (weatherProto is null)
            return true;

        // If the specified weather already exists, just update its duration
        if (weatherEnt != null)
        {
            TryAddWeather(mapUid.Value, weatherProto.Value, out weatherEnt, duration);
            return true;
        }

        // Otherwise, add the specified weather
        return TryAddWeather(mapUid.Value, weatherProto.Value, out weatherEnt, duration);
    }
}
