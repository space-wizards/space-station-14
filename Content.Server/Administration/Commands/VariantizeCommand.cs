using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class VariantizeCommand : IConsoleCommand
{

    public string Command => "variantize";

    public string Description => Loc.GetString("variantize-command-description");

    public string Help => Loc.GetString("variantize-command-help-text");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var mapManager = IoCManager.Resolve<IMapManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        if (!int.TryParse(args[0], out var targetId))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        var gridId = new GridId(targetId);
        if (!mapManager.TryGetGrid(gridId, out var grid))
        {
            shell.WriteError(Loc.GetString("shell-invalid-grid-id"));
            return;
        }
        foreach (var tile in grid.GetAllTiles())
        {
            var def = tile.GetContentTileDefinition();
            var newTile = new Tile(tile.Tile.TypeId, tile.Tile.Flags, random.Pick(def.PlacementVariants));
            grid.SetTile(tile.GridIndices, newTile);
        }
    }
}
