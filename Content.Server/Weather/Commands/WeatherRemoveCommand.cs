using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Weather;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Weather.Commands;

/// <summary>
/// Remove specific weather from map.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class WeatherRemoveCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly WeatherSystem _weather = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    public override string Command => "weatherremove";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-no-arguments"));
            return;
        }

        //MapId parse
        if (!int.TryParse(args[0], out var mapInt))
            return;

        var mapId = new MapId(mapInt);

        if (!_map.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-wrong-map", ("id", mapId.ToString())));
            return;
        }

        //Weather proto parse
        EntProtoId weatherProto = args[1];
        if (!_proto.TryIndex(weatherProto, out _))
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-unknown-proto"));
            return;
        }

        if (!_weather.HasWeather(mapId, weatherProto))
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-no-weather"));
            return;
        }

        _weather.TryRemoveWeather(mapId, weatherProto);
    }


    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");

        if (args.Length == 2) //TODO: dont show ALL weathers here, only weathers applied to selected map
        {
            var opts = new List<CompletionOption>();
            foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.Components.TryGetComponent(_compFactory.GetComponentName<WeatherStatusEffectComponent>(), out _))
                    continue;

                opts.Add(new CompletionOption(proto.ID, proto.Name));
            }

            opts.Add(new CompletionOption("null", Loc.GetString("cmd-weather-null")));
            return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-weather-hint"));
        }

        return CompletionResult.Empty;
    }
}
