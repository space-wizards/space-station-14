using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Radiation.Systems;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class TileRadiationCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;

    public string Command => "tilerad";
    public string Description => "View or clear tile radiation.";
    public string Help => "Usage:\n" +
                          "  tilerad list [mapId]\n" +
                          "  tilerad set <gridUid> <x> <y> <sourceId> <intensity> [slope] [halfLife]\n" +
                          "  tilerad clear <gridUid> <x> <y> [sourceId]\n" +
                          "  tilerad clearall [sourceId]\n" +
                          "  tilerad clearpersistent";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError("Needs a subcommand. See 'help tilerad'.");
            return;
        }

        var radSystem = _entManager.System<RadiationSystem>();
        var subCommand = args[0].ToLower();

        switch (subCommand)
        {
            case "list":
                HandleList(shell, radSystem, _entManager, args);
                break;
            case "set":
                HandleSet(shell, radSystem, _entManager, args);
                break;
            case "clear":
                HandleClear(shell, radSystem, _entManager, args);
                break;
            case "clearall":
                HandleClearAll(shell, radSystem, args);
                break;
            case "clearpersistent":
                HandleClearPersistent(shell, radSystem, args);
                break;
            default:
                shell.WriteError($"Unknown subcommand: {subCommand}");
                break;
        }
    }

    private static void HandleList(IConsoleShell shell, RadiationSystem radSystem, IEntityManager entManager, string[] args)
    {
        var sources = radSystem.GetTileRadiationSources();

        if (sources.Count == 0)
        {
            shell.WriteLine("No tile radiation active.");
            return;
        }

        var xformQuery = entManager.GetEntityQuery<TransformComponent>();

        if (args.Length == 1)
        {
            shell.WriteLine("--- Tile Radiation Overview ---");

            var mapMetrics = new Dictionary<MapId, (int Tiles, int Sources)>();
            foreach (var (spatialKey, tileSources) in sources)
            {
                if (!xformQuery.TryGetComponent(spatialKey.GridUid, out var xform))
                    continue;

                var mapId = xform.MapID;
                if (!mapMetrics.TryGetValue(mapId, out var metric))
                    metric = (0, 0);

                mapMetrics[mapId] = (metric.Tiles + 1, metric.Sources + tileSources.Count);
            }

            foreach (var (mapId, metric) in mapMetrics)
            {
                shell.WriteLine($"Map {mapId}: {metric.Tiles} tiles affected, {metric.Sources} total sources.");
            }
            return;
        }

        if (!int.TryParse(args[1], out var rawMapId))
        {
            shell.WriteError("Invalid MapID.");
            return;
        }

        var targetMap = new MapId(rawMapId);
        shell.WriteLine($"--- Tile Radiation Breakdown for Map {targetMap} ---");

        var entriesFound = false;
        foreach (var (spatialKey, distinctSources) in sources)
        {
            if (!xformQuery.TryGetComponent(spatialKey.GridUid, out var xform) || xform.MapID != targetMap)
                continue;

            entriesFound = true;
            shell.WriteLine($"Grid: {spatialKey.GridUid} | Tile X: {spatialKey.Tile.X}, Y: {spatialKey.Tile.Y}");
            foreach (var data in distinctSources.Values)
            {
                shell.WriteLine($" ID: {data.SourceId} | Rads: {data.Intensity:F2} | HalfLife: {data.HalfLife}s | Slope: {data.Slope} ");
            }
        }

        if (!entriesFound)
        {
            shell.WriteLine($"Map {targetMap} has no tile radiation.");
        }
    }

    private static void HandleSet(IConsoleShell shell, RadiationSystem radSystem, IEntityManager entManager, string[] args)
    {
        if (args.Length < 6 || args.Length > 8)
        {
            shell.WriteError("Usage: tilerad set <gridUid> <x> <y> <sourceId> <intensity> [slope] [halfLife]");
            return;
        }

        if (!NetEntity.TryParse(args[1], out var netGrid) ||
            !int.TryParse(args[2], out var x) ||
            !int.TryParse(args[3], out var y) ||
            !ushort.TryParse(args[4], out var sourceId) ||
            !float.TryParse(args[5], out var intensity))
        {
            shell.WriteError("gridUid, x, y must be valid. intensity must be a float.");
            return;
        }

        var gridUid = entManager.GetEntity(netGrid);
        if (!entManager.EntityExists(gridUid))
        {
            shell.WriteError($"Grid entity {netGrid} does not exist.");
            return;
        }
        var tile = new Vector2i(x, y);

        var slope = 0.5f;
        if (args.Length >= 7 && !float.TryParse(args[6], out slope))
        {
            shell.WriteError("slope must be a float.");
            return;
        }

        var halfLife = -1f;
        if (args.Length == 8 && !float.TryParse(args[7], out halfLife))
        {
            shell.WriteError("halfLife must be a float.");
            return;
        }

        radSystem.SetTileRadiation(gridUid, tile, sourceId, intensity, slope, halfLife, true);
        shell.WriteLine($"Set tile radiation on grid {gridUid} tile ({x}, {y}) with ID '{sourceId}'.");
    }

    private static void HandleClear(IConsoleShell shell, RadiationSystem radSystem, IEntityManager entManager, string[] args)
    {
        if (args.Length < 4 || args.Length > 5)
        {
            shell.WriteError("Usage: tilerad clear <gridUid> <x> <y> [sourceId]");
            return;
        }

        if (!NetEntity.TryParse(args[1], out var netGrid) ||
            !int.TryParse(args[2], out var x) ||
            !int.TryParse(args[3], out var y))
        {
            shell.WriteError("Args must be valid numbers.");
            return;
        }

        var gridUid = entManager.GetEntity(netGrid);
        if (!entManager.EntityExists(gridUid))
        {
            shell.WriteError($"Grid entity {netGrid} does not exist.");
            return;
        }
        var tile = new Vector2i(x, y);

        if (args.Length == 5)
        {
            if (!ushort.TryParse(args[4], out var targetId))
            {
                shell.WriteError("sourceId must be a valid ushort integer.");
                return;
            }
            radSystem.ClearTileRadiation(gridUid, tile, source => source.SourceId == targetId);
            shell.WriteLine($"Cleared tile sources matching ID '{targetId}' on grid {gridUid} tile ({x}, {y}).");
        }
        else
        {
            radSystem.ClearTileRadiation(gridUid, tile, _ => true);
            shell.WriteLine($"Cleared all radiation on grid {gridUid} tile ({x}, {y}).");
        }
    }

    private static void HandleClearAll(IConsoleShell shell, RadiationSystem radSystem, string[] args)
    {
        if (args.Length > 2)
        {
            shell.WriteError("Usage: tilerad clearall [sourceId]");
            return;
        }

        if (args.Length == 2)
        {
            if (!ushort.TryParse(args[1], out var targetId))
            {
                shell.WriteError("sourceId must be a valid ushort integer.");
                return;
            }
            radSystem.ClearAllTileRadiation(source => source.SourceId == targetId);
            shell.WriteLine($"Cleared all tile sources matching ID: {targetId}");
        }
        else
        {
            radSystem.ClearAllTileRadiation(_ => true);
            shell.WriteLine("Cleared all tile radiation sources across all maps.");
        }
    }

    private static void HandleClearPersistent(IConsoleShell shell, RadiationSystem radSystem, string[] args)
    {
        radSystem.ClearAllTileRadiation(source => source.HalfLife == 0f);
        shell.WriteLine("Cleared all persistent tile radiation sources (HalfLife == 0).");
    }
}
