using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Weather;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

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
            StartTime = component.StartTime,
        };
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class WeatherCommand : IConsoleCommand
{
    // Wouldn't you like to know, weather boy.
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public string Command => "weather";
    public string Description => $"Sets the weather for the current map";
    public string Help => $"weather <mapId> <prototype / null>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
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

        if (!_mapManager.MapExists(mapId))
        {
            return;
        }

        var weatherSystem = _entManager.System<WeatherSystem>();

        if (args[1].Equals("null"))
        {
            weatherSystem.SetWeather(mapId, null);
        }
        else if (_protoManager.TryIndex<WeatherPrototype>(args[1], out var weatherProto))
        {
            weatherSystem.SetWeather(mapId, weatherProto);
        }
        else
        {
            shell.WriteError($"Unable to parse weather prototype");
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var options = new List<CompletionOption>();

        if (args.Length == 1)
        {
            options.AddRange(_entManager.EntityQuery<MapComponent>(true).Select(o => new CompletionOption(o.WorldMap.ToString())));
            return CompletionResult.FromHintOptions(options, "Map Id");
        }

        var a = CompletionHelper.PrototypeIDs<WeatherPrototype>(true, _protoManager);
        return CompletionResult.FromHintOptions(a, "Weather prototype");
    }
}
