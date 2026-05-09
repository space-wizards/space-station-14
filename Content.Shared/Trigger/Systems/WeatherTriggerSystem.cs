using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Trigger.Systems;

public sealed partial class WeatherTriggerSystem : XOnTriggerSystem<WeatherOnTriggerComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;

    protected override void OnTrigger(Entity<WeatherOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var xform = Transform(target);

        if (ent.Comp.Weather == null) //Clear weather if nothing is set
        {
            _weather.TrySetWeather(xform.MapID, null, out _);
            return;
        }

        var endTime = ent.Comp.Duration == null ? null : ent.Comp.Duration + _timing.CurTime;

        if (_prototypeManager.Resolve(ent.Comp.Weather, out var weatherPrototype))
            _weather.TrySetWeather(xform.MapID, weatherPrototype, out _, endTime);
    }
}
