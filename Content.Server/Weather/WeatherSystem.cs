using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Weather;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Server.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

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
        args.State = new WeatherComponentState(component.Weather);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void WeatherTwo(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-no-arguments"));
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
            return;

        var mapId = new MapId(mapInt);

        if (!MapManager.MapExists(mapId))
            return;

        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return;

        var weatherComp = EnsureComp<WeatherComponent>(mapUid.Value);

        //Weather Proto parsing
        WeatherPrototype? weather = null;
        if (!args[1].Equals("null"))
        {
            if (!ProtoMan.TryIndex(args[1], out weather))
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-unknown-proto"));
                return;
            }
        }

        //Time parsing
        TimeSpan? endTime = null;
        if (args.Length == 3)
        {
            var curTime = Timing.CurTime;
            if (int.TryParse(args[2], out var durationInt))
            {
                endTime = curTime + TimeSpan.FromSeconds(durationInt);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-wrong-time"));
            }
        }

        SetWeather(mapId, weather, endTime);
    }

    private CompletionResult WeatherCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");

        var a = CompletionHelper.PrototypeIDs<WeatherPrototype>(true, ProtoMan);
        var b = a.Concat(new[] { new CompletionOption("null", Loc.GetString("cmd-weather-null")) });
        return CompletionResult.FromHintOptions(b, Loc.GetString("cmd-weather-hint"));
    }
}
