using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class VariantizeCommand : LocalizedEntityCommands
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
            shell.WriteError($"Failed to parse euid '{args[0]}'.");
            return;
        }

        if (!EntityManager.TryGetComponent(euid, out MapGridComponent? gridComp))
        {
            shell.WriteError($"Euid '{euid}' does not exist or is not a grid.");
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

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.Components<MapGridComponent>(args[0], EntityManager),
                Loc.GetString($"{Command}-hint-map")),
            _ => CompletionResult.Empty,
        };
    }
}
