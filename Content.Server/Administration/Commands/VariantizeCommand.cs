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

        var entMan = IoCManager.Resolve<IEntityManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        if (!EntityUid.TryParse(args[0], out var euid))
        {
            shell.WriteError($"Failed to parse euid '{args[0]}'.");
            return;
        }

        if (!entMan.TryGetComponent(euid, out MapGridComponent? gridComp))
        {
            shell.WriteError($"Euid '{euid}' does not exist or is not a grid.");
            return;
        }

        foreach (var tile in gridComp.GetAllTiles())
        {
            var def = tile.GetContentTileDefinition();
            var newTile = new Tile(tile.Tile.TypeId, tile.Tile.Flags, random.Pick(def.PlacementVariants));
            gridComp.SetTile(tile.GridIndices, newTile);
        }
    }
}
