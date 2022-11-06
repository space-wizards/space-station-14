using Content.Shared.Weather;
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
}
