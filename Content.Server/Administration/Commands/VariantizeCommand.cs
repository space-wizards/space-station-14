using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class VariantizeCommand : LocalizedEntityCommands
{
    public override string Command => "variantize";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var euidNet) || !EntityManager.TryGetEntity(euidNet, out var euid))
        {
            shell.WriteError(Loc.GetString("cmd-variantize-parse-failed", ("arg", args[0])));
            return;
        }

        if (!EntityManager.TryGetComponent(euid, out MapGridComponent? gridComp))
        {
            shell.WriteError(Loc.GetString("cmd-variantize-not-grid", ("euid", euid)));
            return;
        }

        var mapsSystem = EntityManager.System<SharedMapSystem>();
        var tileSystem = EntityManager.System<TileSystem>();
        var turfSystem = EntityManager.System<TurfSystem>();

        foreach (var tile in mapsSystem.GetAllTiles(euid.Value, gridComp))
        {
            var def = turfSystem.GetContentTileDefinition(tile);
            var newTile = new Tile(tile.Tile.TypeId, tile.Tile.Flags, tileSystem.PickVariant(def), tile.Tile.RotationMirroring);
            mapsSystem.SetTile(euid.Value, gridComp, tile.GridIndices, newTile);
        }
    }
}
