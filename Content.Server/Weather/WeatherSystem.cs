using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Weather;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
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

    public string Command => "weather";
    public string Description => $"Sets the weather for the current map";
    public string Help => $"weather <mapId> <prototype / null>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var mapManager = IoCManager.Resolve<IMapManager>();

        if (!int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);

        if (!mapManager.MapExists(mapId))
        {
            return;
        }

        var weatherSystem = entManager.System<WeatherSystem>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (args[1].Equals("null"))
        {
            weatherSystem.SetWeather(mapId, null);
        }
        else if (protoMan.TryIndex<WeatherPrototype>(args[1], out var weatherProto))
        {
            weatherSystem.SetWeather(mapId, weatherProto);
        }
        else
        {
            shell.WriteError($"Yeah nah");
            return;
        }

        // TODO: Autocomplete
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var options = new List<CompletionOption>();

        if (args.Length == 1)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            options.AddRange(mapManager.GetAllMapIds().Select(mapId => new CompletionOption(mapId.ToString())));
            return CompletionResult.FromHintOptions(options, "Map Id");
        }
        else
        {
            var protoManager = IoCManager.Resolve<IPrototypeManager>();
            options.AddRange(protoManager.EnumeratePrototypes<WeatherPrototype>().Select(o => new CompletionOption(o.ID)));
            return CompletionResult.FromHintOptions(options, "Weather prototype");
        }
    }
}
