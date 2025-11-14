using Content.Shared.Weather;
using Robust.Server.GameStates;

namespace Content.Server.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    //I dont really like to PVS override weather entities, but map status effect containers dont PVS-ing out of the box
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeatherStatusEffectComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<WeatherStatusEffectComponent, ComponentShutdown>(OnCompShutdown);
    }

    private void OnCompInit(Entity<WeatherStatusEffectComponent> ent, ref ComponentInit args)
    {
        _pvs.AddGlobalOverride(ent);
    }

    private void OnCompShutdown(Entity<WeatherStatusEffectComponent> ent, ref ComponentShutdown args)
    {
        Audio.Stop(ent.Comp.Stream);
        _pvs.RemoveGlobalOverride(ent);
    }
}
