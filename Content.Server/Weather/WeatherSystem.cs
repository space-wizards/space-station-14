using Content.Shared.Weather;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeatherComponent, ComponentGetState>(OnWeatherGetState);
    }

    private void OnWeatherGetState(EntityUid uid, WeatherComponent component, ref ComponentGetState args)
    {
        args.State = new WeatherComponentState()
        {
            Weather = component.Weather,
            EndTime = component.EndTime,
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // TODO: Active component.
        var curTime = Timing.CurTime;

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
            if (!ProtoMan.TryIndex<WeatherPrototype>(comp.Weather, out var weatherProto))
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
        }
    }

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
}
