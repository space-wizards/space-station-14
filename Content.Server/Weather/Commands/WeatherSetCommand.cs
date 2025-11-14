using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Weather.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class WeatherSetCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override string Command => "weather_set";

    public override string Description => "Removes all weather conditions except the specified one. If the specified weather does not exist on the map, it adds it.";

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

        //Weather proto parse
        EntProtoId? weatherProto = args[1];
        if (args[1] == "null")
            weatherProto = null;
        else if (!_proto.TryIndex(weatherProto, out _))
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-unknown-proto"));
            return;
        }

        //Time parsing
        TimeSpan? duration = null;
        if (args.Length == 3)
        {
            var curTime = _timing.CurTime;
            if (int.TryParse(args[2], out var durationInt))
            {
                duration = curTime + TimeSpan.FromSeconds(durationInt);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-wrong-time"));
            }
        }

        _entMan.System<WeatherSystem>().SetWeather(mapId, weatherProto, duration);
    }


    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");

        if (args.Length == 2)
        {
            var opts = new List<CompletionOption>();
            foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.Components.ContainsKey("WeatherStatusEffect")) //Uhh, iirc we have something like nameof(), but i cant found it.
                    continue;

                opts.Add(new CompletionOption(proto.ID, proto.Name));
            }

            opts.Add(new CompletionOption("null", Loc.GetString("cmd-weather-null")));
            return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-weather-hint"));
        }

        if (args.Length == 3)
            return CompletionResult.FromHint("Duration in seconds (leave empty for infinity duration)");

        return CompletionResult.Empty;
    }
}
