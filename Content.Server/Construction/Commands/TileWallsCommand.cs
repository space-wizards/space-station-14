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
public sealed class TileWallsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "tilewalls";
    public string Description => Loc.GetString("cmd-tilewalls-desc");
    public string Help => Loc.GetString("cmd-tilewalls-help");

    public static readonly ProtoId<ContentTileDefinition> TilePrototypeId = "Plating";
    public static readonly ProtoId<TagPrototype> WallTag = "Wall";
    public static readonly ProtoId<TagPrototype> DiagonalTag = "Diagonal";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        EntityUid? gridId;

        switch (args.Length)
        {
            case 0:
                if (player?.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError(Loc.GetString("cmd-tilewalls-only-player"));
                    return;
                }

                gridId = _entManager.GetComponent<TransformComponent>(playerEntity).GridUid;
                break;
            case 1:
                if (!NetEntity.TryParse(args[0], out var idNet) || !_entManager.TryGetEntity(idNet, out var id))
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

        if (!_entManager.TryGetComponent(gridId, out MapGridComponent? grid))
        {
            shell.WriteError(Loc.GetString("cmd-tilewalls-no-grid", ("gridId", (gridId?.ToString() ?? string.Empty))));
            return;
        }

        if (!_entManager.EntityExists(gridId))
        {
            shell.WriteError(Loc.GetString("cmd-tilewalls-grid-no-entity", ("gridId", (gridId?.ToString() ?? string.Empty))));
            return;
        }

        var tagSystem = _entManager.EntitySysManager.GetEntitySystem<TagSystem>();
        var underplating = _tileDefManager[TilePrototypeId];
        var underplatingTile = new Tile(underplating.TileId);
        var changed = 0;
        var enumerator = _entManager.GetComponent<TransformComponent>(gridId.Value).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!_entManager.EntityExists(child))
            {
                continue;
            }

            if (!tagSystem.HasTag(child, WallTag))
            {
                continue;
            }

            if (tagSystem.HasTag(child, DiagonalTag))
            {
                continue;
            }

            var childTransform = _entManager.GetComponent<TransformComponent>(child);

            if (!childTransform.Anchored)
            {
                continue;
            }

            var mapSystem = _entManager.System<MapSystem>();
            var tile = mapSystem.GetTileRef(gridId.Value, grid, childTransform.Coordinates);
            var tileDef = (ContentTileDefinition)_tileDefManager[tile.Tile.TypeId];

            if (tileDef.ID == TilePrototypeId)
            {
                continue;
            }

            mapSystem.SetTile(gridId.Value, grid, childTransform.Coordinates, underplatingTile);
            changed++;
        }

        shell.WriteLine(Loc.GetString("cmd-tilewalls-changed", ("changed", (object)changed)));
    }
}
