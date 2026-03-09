using Content.Shared.Weather;
using Robust.Server.GameStates;

namespace Content.Server.Weather;

public sealed partial class WeatherSystem : SharedWeatherSystem
{
    //I dont really like to PVS override weather entities, but map status effect containers dont PVS-ing out of the box
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitEffects();

        SubscribeLocalEvent<WeatherStatusEffectComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<WeatherStatusEffectComponent, ComponentShutdown>(OnCompShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateEffects(frameTime);
    }

    private void OnCompInit(Entity<WeatherStatusEffectComponent> ent, ref ComponentInit args)
    {
        // The map entitiy itself is networked by PVS if the player is on that map but not anything inside a container,
        // So we need to add an overridce to make sure the client sees it.
        _pvs.AddGlobalOverride(ent);
    }

    private void OnCompShutdown(Entity<WeatherStatusEffectComponent> ent, ref ComponentShutdown args)
    {
        _pvs.RemoveGlobalOverride(ent);
    }
}
