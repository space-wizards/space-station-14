using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weather;

public abstract class SharedWeatherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("weather");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        // TODO: Active component.
        var curTime = _timing.CurTime;

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
            if (!_protoMan.TryIndex<WeatherPrototype>(comp.Weather, out var weatherProto))
            {
                // TODO: LOG
                EndWeather(comp);
                continue;
            }

            var remainingTime = endTime - curTime;

            // Shutting down
            if (remainingTime < weatherProto.ShutdownTime)
            {
                SetState(comp, WeatherState.Ending);
                continue;
            }

            var startTime = comp.StartTime + pauseTime;
            var elapsed = _timing.CurTime - startTime;

            // Starting up
            if (elapsed < weatherProto.StartupTime)
            {
                SetState(comp, WeatherState.Starting);
                continue;
            }

            // Running
            Run(weatherProto, comp.State);
        }
    }

    public void SetWeather(MapId mapId, WeatherPrototype? weather)
    {
        var weatherComp = EnsureComp<WeatherComponent>(MapManager.GetMapEntityId(mapId));
        EndWeather(weatherComp);

        if (weather != null)
            StartWeather(weatherComp, weather);
    }

    protected virtual void Run(WeatherPrototype weather, WeatherState state) {}

    private void StartWeather(WeatherComponent component, WeatherPrototype weather)
    {
        Sawmill.Debug($"Starting weather {weather.ID}");
        component.Weather = weather.ID;
        var duration = _random.Next(weather.DurationMinimum, weather.DurationMaximum);
        component.EndTime = _timing.CurTime + duration;
        component.StartTime = _timing.CurTime;
        Dirty(component);
    }

    private void EndWeather(WeatherComponent component)
    {
        Sawmill.Debug($"Stopping weather {component.Weather}");
        component.Weather = null;
        component.StartTime = TimeSpan.Zero;
        component.EndTime = TimeSpan.Zero;
        Dirty(component);
    }

    private void SetState(WeatherComponent component, WeatherState state)
    {
        if (component.State.Equals(state))
            return;

        component.State = state;
        // TODO: Run specific logic.
        Sawmill.Debug($"Set weather state for {ToPrettyString(component.Owner)} to {state}");
    }

    [Serializable, NetSerializable]
    protected sealed class WeatherComponentState : ComponentState
    {
        public string? Weather;
        public TimeSpan StartTime;
        public TimeSpan EndTime;
    }
}
