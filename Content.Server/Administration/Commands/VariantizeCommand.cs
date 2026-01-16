using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class VariantizeCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly TileSystem _tile = default!;

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

        foreach (var tile in _map.GetAllTiles(euid.Value, gridComp))
        {
            var def = _turf.GetContentTileDefinition(tile);
            var newTile = new Tile(tile.Tile.TypeId, tile.Tile.Flags, _tile.PickVariant(def), tile.Tile.RotationMirroring);
            _map.SetTile(euid.Value, gridComp, tile.GridIndices, newTile);
        }
    }
}
