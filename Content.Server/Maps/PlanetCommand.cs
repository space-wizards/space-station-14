using System.Linq;
using Content.Server.Administration;
using Content.Server.Parallax;
using Content.Shared.Administration;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

/// <summary>
/// Converts the supplied map into a "planet" with defaults.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class PlanetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public string Command => "planet";
    public string Description => Loc.GetString("cmd-planet-desc");
    public string Help => Loc.GetString("cmd-planet-help", ("command", Command));
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString($"cmd-planet-args"));
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            shell.WriteError(Loc.GetString($"cmd-planet-map", ("map", mapInt)));
            return;
        }

        var mapId = new MapId(mapInt);
        var map = _entManager.System<SharedMapSystem>();

        if (!map.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString($"cmd-planet-map", ("map", mapId)));
            return;
        }

        if (!_protoManager.TryIndex<BiomeTemplatePrototype>(args[1], out var biomeTemplate))
        {
            shell.WriteError(Loc.GetString("cmd-planet-map-prototype", ("prototype", args[1])));
            return;
        }

        var biomeSystem = _entManager.System<BiomeSystem>();
        var mapUid = map.GetMapOrInvalid(mapId);
        biomeSystem.EnsurePlanet(mapUid, biomeTemplate);

        shell.WriteLine(Loc.GetString("cmd-planet-success", ("mapId", mapId)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entManager), "Map Id");

        if (args.Length == 2)
        {
            var options = _protoManager.EnumeratePrototypes<BiomeTemplatePrototype>()
                .Select(o => new CompletionOption(o.ID, "Biome"));
            return CompletionResult.FromOptions(options);
        }

        return CompletionResult.Empty;
    }
}
