using Content.Shared.Maps;
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
    [Dependency] private   readonly IRobustRandom _random = default!;
    [Dependency] private   readonly ITileDefinitionManager _tileDefManager = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("weather");
        SubscribeLocalEvent<WeatherComponent, EntityUnpausedEvent>(OnWeatherUnpaused);
    }

    private void OnWeatherUnpaused(EntityUid uid, WeatherComponent component, ref EntityUnpausedEvent args)
    {
        component.EndTime += args.PausedTime;
    }

    public bool CanWeatherAffect(MapGridComponent grid, TileRef tileRef, EntityQuery<PhysicsComponent> bodyQuery)
    {
        if (tileRef.Tile.IsEmpty)
            return true;

        var tileDef = (ContentTileDefinition) _tileDefManager[tileRef.Tile.TypeId];

        if (!tileDef.Weather)
            return false;

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var curTime = Timing.CurTime;

        foreach (var (comp, metadata) in EntityQuery<WeatherComponent, MetaDataComponent>())
        {
            if (comp.Weather == null)
                continue;

            var uid = comp.Owner;
            var endTime = comp.EndTime;

            // Ended
            if (endTime < curTime)
            {
                EndWeather(comp);
                continue;
            }

            // Admin messed up or the likes.
            if (!ProtoMan.TryIndex<WeatherPrototype>(comp.Weather, out var weatherProto))
            {
                Sawmill.Error($"Unable to find weather prototype for {comp.Weather}, ending!");
                EndWeather(comp);
                continue;
            }

            var remainingTime = endTime - curTime;

            // Shutting down
            if (remainingTime < weatherProto.ShutdownTime)
            {
                SetState(uid, comp, WeatherState.Ending, weatherProto);
            }
            // Starting up
            else
            {
                var startTime = comp.StartTime;
                var elapsed = Timing.CurTime - startTime;

                if (elapsed < weatherProto.StartupTime)
                {
                    SetState(uid, comp, WeatherState.Starting, weatherProto);
                }
            }

            // Run whatever code we need.
            Run(uid, comp, weatherProto, comp.State, frameTime);
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
    protected virtual void Run(EntityUid uid, WeatherComponent component, WeatherPrototype weather, WeatherState state, float frameTime) {}

    protected void StartWeather(WeatherComponent component, WeatherPrototype weather)
    {
        component.Weather = weather.ID;
        // TODO: ENGINE PR
        var duration = _random.NextDouble(weather.DurationMinimum.TotalSeconds, weather.DurationMaximum.TotalSeconds);
        component.EndTime = Timing.CurTime + TimeSpan.FromSeconds(duration);
        component.StartTime = Timing.CurTime;
        DebugTools.Assert(component.State == WeatherState.Invalid);
        Dirty(component);
    }

    protected virtual void EndWeather(WeatherComponent component)
    {
        component.Stream?.Stop();
        component.Stream = null;
        component.Weather = null;
        component.StartTime = TimeSpan.Zero;
        component.EndTime = TimeSpan.Zero;
        component.State = WeatherState.Invalid;
        Dirty(component);
    }

    protected virtual bool SetState(EntityUid uid, WeatherComponent component, WeatherState state, WeatherPrototype prototype)
    {
        if (component.State.Equals(state))
            return false;

        component.State = state;
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
