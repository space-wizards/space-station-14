using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class VariantizeCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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

        foreach (var tile in gridComp.GetAllTiles())
        {
            var def = tile.GetContentTileDefinition();
            var newTile = new Tile(tile.Tile.TypeId, tile.Tile.Flags, def.PickVariant(_random));
            gridComp.SetTile(tile.GridIndices, newTile);
        }
    }
}
