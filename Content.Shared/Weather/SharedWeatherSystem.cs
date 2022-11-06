using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weather;

public abstract class SharedWeatherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // TODO: Active component.
        var curTime = _timing.CurTime;

        foreach (var comp in EntityQuery<WeatherComponent>())
        {
            if (comp.Weather == null)
                continue;

            // Ended
            if (comp.EndTime < curTime)
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

            var remainingTime = comp.EndTime - curTime;

            // Shutting down
            if (remainingTime < weatherProto.ShutdownTime)
            {
                SetState(comp, WeatherState.Ending);
                continue;
            }

            // Starting up
            if (remainingTime > (weatherProto.ShutdownTime +
                                 (comp.Duration - weatherProto.ShutdownTime - weatherProto.StartupTime)))
            {
                SetState(comp, WeatherState.Starting);
                continue;
            }

            // Running
            Run(weatherProto, comp.State);
        }
    }

    protected virtual void Run(WeatherPrototype weather, WeatherState state) {}

    private void EndWeather(WeatherComponent component)
    {
        component.Weather = null;
        component.EndTime = TimeSpan.Zero;
        Dirty(component);
    }

    private void SetState(WeatherComponent component, WeatherState state)
    {
        if (component.State.Equals(state))
            return;

        component.State = state;
        // TODO: Run specific logic.
    }

    [Serializable, NetSerializable]
    protected sealed class WeatherComponentState : ComponentState
    {
        public string? Weather;
        public TimeSpan Duration;
        public TimeSpan EndTime;
    }
}
