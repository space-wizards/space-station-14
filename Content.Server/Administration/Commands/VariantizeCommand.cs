using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class VariantizeCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

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

        if (!NetEntity.TryParse(args[0], out var euidNet) || !_entManager.TryGetEntity(euidNet, out var euid))
        {
            shell.WriteError($"Failed to parse euid '{args[0]}'.");
            return;
        }

        if (!_entManager.TryGetComponent(euid, out MapGridComponent? gridComp))
        {
            shell.WriteError($"Euid '{euid}' does not exist or is not a grid.");
            return;
        }

        var mapsSystem = _entManager.System<SharedMapSystem>();
        var tileSystem = _entManager.System<TileSystem>();

        foreach (var tile in mapsSystem.GetAllTiles(euid.Value, gridComp))
        {
            var def = tile.GetContentTileDefinition(_tileDefManager);
            var newTile = new Tile(tile.Tile.TypeId, tile.Tile.Flags, tileSystem.PickVariant(def));
            mapsSystem.SetTile(euid.Value, gridComp, tile.GridIndices, newTile);
        }
    }
}
