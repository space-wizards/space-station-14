using Content.Shared.Interaction;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Content.Shared.Farming;

namespace Content.Server.Farming
{
    public sealed partial class SharedPloughingSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly ITileDefinitionManager _tileManager = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PloughToolComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<PloughToolComponent, PloughDoAfterEvent>(OnDoAfter);
        }

        private void OnAfterInteract(Entity<PloughToolComponent> ent, ref AfterInteractEvent args)
        {
            if (args.Handled || args.Target != null || !args.CanReach)
                return;

            var comp = ent.Comp;
            var user = args.User;
            var clickLocation = args.ClickLocation;

            // Grass tiles that can be turned into dirt tiles
            var grassTiles = new HashSet<string>
    {
        "FloorGrass",
        "FloorGrassJungle",
        "FloorGrassDark",
        "FloorGrassLight",
        "FloorAstroGrass",
        "FloorPlanetGrass",
        "FloorJungleAstroGrass",
        "FloorMowedAstroGrass"
    };

            // Get clicked grid
            var gridUid = _transform.GetGrid(args.ClickLocation);
            if (!gridUid.HasValue || !TryComp<MapGridComponent>(gridUid.Value, out var grid))
            {
                return;
            }

            // Get tile coords
            var snapPos = grid.TileIndicesFor(clickLocation);
            var tileRef = _map.GetTileRef(gridUid.Value, grid, snapPos);
            var tileDef = (ContentTileDefinition)_tileManager[tileRef.Tile.TypeId];

            PloughActionType actionType;
            string popupMessage;

            // Ploughing dirt or grass?
            if (tileDef.ID == "FloorDirt")
            {
                actionType = PloughActionType.Plough;
                popupMessage = "You begin plowing the soil.";
            }
            else if (grassTiles.Contains(tileDef.ID))
            {
                actionType = PloughActionType.ClearGrass;
                popupMessage = "You begin clearing the grass.";
            }
            else
            {
                return; // Cant plough this tile
            }

            var delay = comp.Delay;
            var netGridUid = GetNetEntity(gridUid.Value);
            var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new PloughDoAfterEvent(netGridUid, snapPos, actionType), ent)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

            if (_doAfter.TryStartDoAfter(doAfterArgs))
            {
                _popup.PopupEntity(popupMessage, ent, user);
                args.Handled = true;
            }
        }

        private void OnDoAfter(Entity<PloughToolComponent> ent, ref PloughDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled)
                return;

            var gridUid = GetEntity(args.GridUid);
            if (!TryComp<MapGridComponent>(gridUid, out var grid))
                return;

            var snapPos = args.SnapPos;
            var coordinates = grid.GridTileToLocal(snapPos);

            if (args.ActionType == PloughActionType.Plough)
            {
                // Create entity ploughedField
                var ploughedField = Spawn("ploughedField", coordinates);
                _popup.PopupEntity("You finish plowing the field.", ent, args.User);
            }
            else if (args.ActionType == PloughActionType.ClearGrass)
            {
                // Turns grass fields into dirt
                var dirtTile = _tileManager["FloorDirt"];
                var newTile = new Tile(dirtTile.TileId);
                _map.SetTile(gridUid, grid, snapPos, newTile);
                _popup.PopupEntity("You finish clearing the grass, turning it into dirt.", ent, args.User);
            }

            args.Handled = true;
        }
    }
}