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

            // Check if tile is ploughable or transformable
            if (tileDef.ID == "FloorDirt")
            {
                // Logic for FloorDirt: start DoAfter to create ploughedField
                var delay = comp.Delay;
                var netGridUid = GetNetEntity(gridUid.Value);
                var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new PloughDoAfterEvent(netGridUid, snapPos), ent)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = true
                };

                if (_doAfter.TryStartDoAfter(doAfterArgs))
                {
                    _popup.PopupEntity("You begin plowing the soil.", ent, user);
                    args.Handled = true;
                }
            }
            // Clear grass, turning into floor dirt
            else if (grassTiles.Contains(tileDef.ID))
            {
                // Transform grass tile to FloorDirt
                var dirtTile = _tileManager["FloorDirt"];
                var newTile = new Tile(dirtTile.TileId);
                _map.SetTile(gridUid.Value, grid, snapPos, newTile);

                _popup.PopupEntity("You clear the grass, turning it into dirt.", ent, user);
                args.Handled = true;
            }
            else
            {
                _popup.PopupEntity("This tile cannot be plowed or transformed.", ent, user);
            }
        }

        private void OnDoAfter(Entity<PloughToolComponent> ent, ref PloughDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled)
                return;

            var gridUid = GetEntity(args.GridUid);
            if (!TryComp<MapGridComponent>(gridUid, out var grid))
                return;

            var coordinates = grid.GridTileToLocal(args.SnapPos);
            var ploughedField = Spawn("ploughedField", coordinates);
            _popup.PopupEntity("You finish plowing the field.", ent, args.User);
            args.Handled = true;
        }
    }
}