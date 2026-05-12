using System.Linq;
using Content.Server.Administration;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Parallax;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Maps;

/// <summary>
/// Converts the supplied map into a "planet" with defaults.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class PlanetCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override string Command => "planet";
    public override string Description => Loc.GetString("cmd-planet-desc");
    public override string Help => Loc.GetString("cmd-planet-help", ("command", Command));
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
        if (!_map.MapExists(mapId))
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
        var mapUid = _map.GetMapOrInvalid(mapId);
        biomeSystem.EnsurePlanet(mapUid, biomeTemplate);

        shell.WriteLine(Loc.GetString("cmd-planet-success", ("mapId", mapId)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
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
