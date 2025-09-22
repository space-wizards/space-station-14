using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;


namespace Content.Shared.Trigger.Systems;

public sealed class WeatherTriggerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeatherOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<WeatherOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        if (args.User == null)
            return;

        var xform = Transform(args.User.Value);

        _weather.SetWeather(xform.MapID, _prototypeManager.Index(ent.Comp.Weather), null);
    }
}
