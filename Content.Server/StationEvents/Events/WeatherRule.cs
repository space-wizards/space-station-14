using Content.Server.StationEvents.Components;
using Content.Server.Weather;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class WeatherRule : StationEventSystem<WeatherRuleComponent>
{
    [Dependency] private readonly WeatherSystem _weather = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, WeatherRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        if (!TryComp<StationDataComponent>(chosenStation, out var stationData))
            return;

        var grid = StationSystem.GetLargestGrid((chosenStation.Value, stationData));

        if (grid is null)
            return;
        
        var map = Transform(grid.Value).MapID;
        var duration = _random.Next(component.MinDuration, component.MaxDuration);
        _weather.TryAddWeather(map, component.Weather, out _, duration);
    }
}
