using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class TileWallsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    // ReSharper disable once StringLiteralTypo
    public override string Command => "tilewalls";

    public static readonly ProtoId<ContentTileDefinition> TilePrototypeId = "Plating";
    public static readonly ProtoId<TagPrototype> WallTag = "Wall";
    public static readonly ProtoId<TagPrototype> DiagonalTag = "Diagonal";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        EntityUid? gridId;

        switch (args.Length)
        {
            case 0:
                if (player?.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
                    return;
                }

                gridId = EntityManager.GetComponent<TransformComponent>(playerEntity).GridUid;
                break;
            case 1:
                if (!NetEntity.TryParse(args[0], out var idNet) || !EntityManager.TryGetEntity(idNet, out var id))
                {
                    shell.WriteError(Loc.GetString("cmd-tilewalls-invalid-entity", ("entity", args[0])));
                    return;
                }

                gridId = id;
                break;
            default:
                shell.WriteLine(Help);
                return;
        }

        if (!EntityManager.TryGetComponent(gridId, out MapGridComponent? grid))
        {
            shell.WriteError(Loc.GetString("cmd-tilewalls-no-grid", ("gridId", (gridId?.ToString() ?? string.Empty))));
            return;
        }

        if (!EntityManager.EntityExists(gridId))
        {
            shell.WriteError(Loc.GetString("cmd-tilewalls-grid-no-entity", ("gridId", (gridId?.ToString() ?? string.Empty))));
            return;
        }

        var underplating = _tileDefManager[TilePrototypeId];
        var underplatingTile = new Tile(underplating.TileId);
        var changed = 0;
        var enumerator = EntityManager.GetComponent<TransformComponent>(gridId.Value).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!EntityManager.EntityExists(child))
            {
                continue;
            }

            if (!_tagSystem.HasTag(child, WallTag))
            {
                continue;
            }

            if (_tagSystem.HasTag(child, DiagonalTag))
            {
                continue;
            }

            var childTransform = EntityManager.GetComponent<TransformComponent>(child);

            if (!childTransform.Anchored)
            {
                continue;
            }

            var tile = _mapSystem.GetTileRef(gridId.Value, grid, childTransform.Coordinates);
            var tileDef = (ContentTileDefinition)_tileDefManager[tile.Tile.TypeId];

            if (tileDef.ID == TilePrototypeId)
            {
                continue;
            }

            _mapSystem.SetTile(gridId.Value, grid, childTransform.Coordinates, underplatingTile);
            changed++;
        }

        shell.WriteLine(Loc.GetString("cmd-tilewalls-changed", ("changed", changed)));
    }
}
