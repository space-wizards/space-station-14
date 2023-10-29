using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weather;

public abstract class SharedWeatherSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] private   readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private   readonly MetaDataSystem _metadata = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("weather");
        SubscribeLocalEvent<WeatherComponent, EntityUnpausedEvent>(OnWeatherUnpaused);
    }

    private void OnWeatherUnpaused(EntityUid uid, WeatherComponent component, ref EntityUnpausedEvent args)
    {
        foreach (var weather in component.Weather.Values)
        {
            weather.StartTime += args.PausedTime;

            if (weather.EndTime != null)
                weather.EndTime = weather.EndTime.Value + args.PausedTime;
        }
    }

    public bool CanWeatherAffect(
        MapGridComponent grid,
        TileRef tileRef,
        EntityQuery<IgnoreWeatherComponent> weatherIgnoreQuery,
        EntityQuery<PhysicsComponent> bodyQuery)
    {
        if (tileRef.Tile.IsEmpty)
            return true;

        var tileDef = (ContentTileDefinition) _tileDefManager[tileRef.Tile.TypeId];

        if (!tileDef.Weather)
            return false;

        var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(tileRef.GridIndices);

        while (anchoredEnts.MoveNext(out var ent))
        {
            if (!weatherIgnoreQuery.HasComponent(ent.Value) &&
                bodyQuery.TryGetComponent(ent, out var body) &&
                body.Hard &&
                body.CanCollide)
            {
                return false;
            }
        }

        return true;

    }

    public float GetPercent(WeatherData component, EntityUid mapUid)
    {
        var pauseTime = _metadata.GetPauseTime(mapUid);
        var elapsed = Timing.CurTime - (component.StartTime + pauseTime);
        var duration = component.Duration;
        var remaining = duration - elapsed;
        float alpha;

        if (remaining < WeatherComponent.ShutdownTime)
        {
            alpha = (float) (remaining / WeatherComponent.ShutdownTime);
        }
        else if (elapsed < WeatherComponent.StartupTime)
        {
            alpha = (float) (elapsed / WeatherComponent.StartupTime);
        }
        else
        {
            alpha = 1f;
        }

        return alpha;
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var curTime = Timing.CurTime;

        var query = EntityQueryEnumerator<WeatherComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Weather.Count == 0)
                continue;

            foreach (var (proto, weather) in comp.Weather)
            {
                var endTime = weather.EndTime;

                // Ended
                if (endTime != null && endTime < curTime)
                {
                    EndWeather(uid, comp, proto);
                    continue;
                }

                var remainingTime = endTime - curTime;

                // Admin messed up or the likes.
                if (!ProtoMan.TryIndex<WeatherPrototype>(proto, out var weatherProto))
                {
                    Sawmill.Error($"Unable to find weather prototype for {comp.Weather}, ending!");
                    EndWeather(uid, comp, proto);
                    continue;
                }

                // Shutting down
                if (endTime != null && remainingTime < WeatherComponent.ShutdownTime)
                {
                    SetState(WeatherState.Ending, comp, weather, weatherProto);
                }
                // Starting up
                else
                {
                    var startTime = weather.StartTime;
                    var elapsed = Timing.CurTime - startTime;

                    if (elapsed < WeatherComponent.StartupTime)
                    {
                        SetState(WeatherState.Starting, comp, weather, weatherProto);
                    }
                }

                // Run whatever code we need.
                Run(uid, weather, weatherProto, frameTime);
            }
        }
    }

    /// <summary>
    /// Shuts down all existing weather and starts the new one if applicable.
    /// </summary>
    public void SetWeather(MapId mapId, WeatherPrototype? proto, TimeSpan? endTime)
    {
        var weatherComp = EnsureComp<WeatherComponent>(MapManager.GetMapEntityId(mapId));

        foreach (var (eProto, weather) in weatherComp.Weather)
        {
            // Reset cooldown if it's an existing one.
            if (eProto == proto?.ID)
            {
                weather.EndTime = endTime;

                if (weather.State == WeatherState.Ending)
                    weather.State = WeatherState.Running;

                Dirty(weatherComp);
                continue;
            }

            // Speedrun
            var end = Timing.CurTime + WeatherComponent.ShutdownTime;

            if (weather.EndTime == null || weather.EndTime > end)
            {
                weather.EndTime = end;
                Dirty(weatherComp);
            }
        }

        if (proto != null)
            StartWeather(weatherComp, proto, endTime);
    }

    /// <summary>
    /// Run every tick when the weather is running.
    /// </summary>
    protected virtual void Run(EntityUid uid, WeatherData weather, WeatherPrototype weatherProto, float frameTime) {}

    protected void StartWeather(WeatherComponent component, WeatherPrototype weather, TimeSpan? endTime)
    {
        if (component.Weather.ContainsKey(weather.ID))
            return;

        var data = new WeatherData()
        {
            StartTime = Timing.CurTime,
            EndTime = endTime,
        };

        component.Weather.Add(weather.ID, data);
        Dirty(component);
    }

    protected virtual void EndWeather(EntityUid uid, WeatherComponent component, string proto)
    {
        if (!component.Weather.TryGetValue(proto, out var data))
            return;

        data.Stream?.Stop();
        data.Stream = null;
        component.Weather.Remove(proto);
        Dirty(component);
    }

    protected virtual bool SetState(WeatherState state, WeatherComponent component, WeatherData weather, WeatherPrototype weatherProto)
    {
        if (weather.State.Equals(state))
            return false;

        weather.State = state;
        Dirty(component);
        return true;
    }

    [Serializable, NetSerializable]
    protected sealed class WeatherComponentState : ComponentState
    {
        public Dictionary<string, WeatherData> Weather;

        public WeatherComponentState(Dictionary<string, WeatherData> weather)
        {
            Weather = weather;
        }
    }
}
