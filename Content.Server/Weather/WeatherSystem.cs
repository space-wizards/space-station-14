using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Weather;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IConsoleHost _console = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeatherComponent, ComponentGetState>(OnWeatherGetState);
        _console.RegisterCommand("weather",
            Loc.GetString("cmd-weather-desc"),
            Loc.GetString("cmd-weather-help"),
            WeatherTwo,
            WeatherCompletion);
    }

    private void OnWeatherGetState(EntityUid uid, WeatherComponent component, ref ComponentGetState args)
    {
        args.State = new WeatherComponentState()
        {
            Weather = component.Weather,
            EndTime = component.EndTime,
            StartTime = component.StartTime,
        };
    }

    [AdminCommand(AdminFlags.Fun)]
    private void WeatherTwo(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
        {
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);

        if (!MapManager.MapExists(mapId))
        {
            return;
        }

        if (args[1].Equals("null"))
        {
            SetWeather(mapId, null);
        }
        else if (ProtoMan.TryIndex<WeatherPrototype>(args[1], out var weatherProto))
        {
            SetWeather(mapId, weatherProto);
        }
        else
        {
            shell.WriteError($"Unable to parse weather prototype");
        }
    }

    private CompletionResult WeatherCompletion(IConsoleShell shell, string[] args)
    {
        var options = new List<CompletionOption>();

        if (args.Length == 1)
        {
            options.AddRange(EntityQuery<MapComponent>(true).Select(o => new CompletionOption(o.WorldMap.ToString())));
            return CompletionResult.FromHintOptions(options, "Map Id");
        }

        var a = CompletionHelper.PrototypeIDs<WeatherPrototype>(true, ProtoMan);
        return CompletionResult.FromHintOptions(a, Loc.GetString("cmd-weather-hint"));
    }
}
