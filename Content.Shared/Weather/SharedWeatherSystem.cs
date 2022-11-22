using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weather;

public abstract class SharedWeatherSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("weather");
    }

    public bool CanWeatherAffect(MapGridComponent grid, TileRef tileRef, WeatherPrototype weatherProto, EntityQuery<PhysicsComponent> bodyQuery)
    {
        if (tileRef.Tile.IsEmpty)
            return true;

        var tileDef = _tileDefManager[tileRef.Tile.TypeId];

        if (weatherProto.Tiles.Contains(tileDef.ID))
        {
            var anchoredEnts = grid.GetAnchoredEntitiesEnumerator(tileRef.GridIndices);

            while (anchoredEnts.MoveNext(out var ent))
            {
                if (bodyQuery.TryGetComponent(ent, out var body) && body.CanCollide)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        // TODO: Active component.
        var curTime = Timing.CurTime;

        foreach (var (comp, metadata) in EntityQuery<WeatherComponent, MetaDataComponent>())
        {
            if (comp.Weather == null)
                continue;

            var pauseTime = _metadata.GetPauseTime(comp.Owner, metadata);
            var endTime = comp.EndTime + pauseTime;

            // Ended
            if (endTime < curTime)
            {
                EndWeather(comp);
                continue;
            }

            // Admin messed up or the likes.
            if (!ProtoMan.TryIndex<WeatherPrototype>(comp.Weather, out var weatherProto))
            {
                // TODO: LOG
                EndWeather(comp);
                continue;
            }

            var remainingTime = endTime - curTime;

            // Shutting down
            if (remainingTime < weatherProto.ShutdownTime)
            {
                SetState(comp, WeatherState.Ending, weatherProto);
            }
            // Starting up
            else
            {
                var startTime = comp.StartTime + pauseTime;
                var elapsed = Timing.CurTime - startTime;

                if (elapsed < weatherProto.StartupTime)
                {
                    SetState(comp, WeatherState.Starting, weatherProto);
                }
            }

            // Run whatever code we need.
            Run(comp, weatherProto, comp.State);
        }
    }

    public void SetWeather(MapId mapId, WeatherPrototype? weather)
    {
        var weatherComp = EnsureComp<WeatherComponent>(MapManager.GetMapEntityId(mapId));
        EndWeather(weatherComp);

        if (weather != null)
            StartWeather(weatherComp, weather);
    }

    /// <summary>
    /// Run every tick when the weather is running.
    /// </summary>
    protected virtual void Run(WeatherComponent component, WeatherPrototype weather, WeatherState state) {}

    protected void StartWeather(WeatherComponent component, WeatherPrototype weather)
    {
        Sawmill.Debug($"Starting weather {weather.ID}");
        component.Weather = weather.ID;
        // TODO: ENGINE PR
        var duration = _random.NextDouble(weather.DurationMinimum.TotalSeconds, weather.DurationMaximum.TotalSeconds);
        component.EndTime = Timing.CurTime + TimeSpan.FromSeconds(duration);
        component.StartTime = Timing.CurTime;
        DebugTools.Assert(component.State == WeatherState.Invalid);
        Dirty(component);
    }

    protected void EndWeather(WeatherComponent component)
    {
        component.Stream?.Stop();
        component.Stream = null;
        Sawmill.Debug($"Stopping weather {component.Weather}");
        component.Weather = null;
        component.StartTime = TimeSpan.Zero;
        component.EndTime = TimeSpan.Zero;
        component.State = WeatherState.Invalid;
        Dirty(component);
    }

    protected virtual bool SetState(WeatherComponent component, WeatherState state, WeatherPrototype prototype)
    {
        if (component.State.Equals(state))
            return false;

        component.State = state;
        Sawmill.Debug($"Set weather state for {ToPrettyString(component.Owner)} to {state}");
        return true;
    }

    [Serializable, NetSerializable]
    protected sealed class WeatherComponentState : ComponentState
    {
        public string? Weather;
        public TimeSpan StartTime;
        public TimeSpan EndTime;
    }
}
