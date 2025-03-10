using System.Numerics;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Impstation.CosmicCult.Visuals;

public sealed class MonumentPlacementPreviewOverlay : Overlay
{
    private readonly IEntityManager _entityManager;
    private readonly IPlayerManager _playerManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly TransformSystem _transformSystem;
    private readonly SharedMapSystem _mapSystem;
    private readonly ITileDefinitionManager _tileDef;
    private readonly EntityLookupSystem _lookup;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    private readonly ShaderInstance _shader;

    //evil huge ctor because doing iocmanager stuff was killing the client for some reason
    public MonumentPlacementPreviewOverlay(IEntityManager entityManager, IPlayerManager playerManager, SpriteSystem spriteSystem, TransformSystem transformSystem, SharedMapSystem mapSystem, ITileDefinitionManager tileDef, EntityLookupSystem lookup, IPrototypeManager protoMan)
    {
        _entityManager = entityManager;
        _playerManager = playerManager;
        _spriteSystem = spriteSystem;
        _transformSystem = transformSystem;
        _mapSystem = mapSystem;
        _tileDef = tileDef;
        _lookup = lookup;
        _shader = protoMan.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(_playerManager.LocalEntity, out var transformComp))
            return;

        if (!_entityManager.TryGetComponent<MapGridComponent>(transformComp.GridUid, out var grid))
            return;

        if (!_entityManager.TryGetComponent<TransformComponent>(transformComp.ParentUid, out var parentTransform))
            return;

        var tex = new SpriteSpecifier.Texture(new ("_Impstation/CosmicCult/Tileset/monument.rsi/stage1.png"));

        var worldHandle = args.WorldHandle;

        //snap the preview to the tile we'll be spawning the monument on
        var localTile = _mapSystem.GetTileRef(transformComp.GridUid.Value, grid, transformComp.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, 1);
        var snappedCoords = _mapSystem.ToCenterCoordinates(transformComp.GridUid.Value, targetIndices, grid);

        //set the colour based on if the target tile is valid or not todo make this something else? like a toggle in a shader or so? that's for later anyway
        var color = VerifyPlacement(transformComp) ? Color.Green : Color.Red;

        worldHandle.SetTransform(parentTransform.LocalMatrix);

        worldHandle.UseShader(_shader);
        worldHandle.DrawTexture(_spriteSystem.Frame0(tex), snappedCoords.Position - new Vector2(1.5f, 0.5f), color); //needs the offset to render in the proper position
        worldHandle.UseShader(null);
    }

    //copied out from the ability code todo update this & the ability to check the snapped position instead of the raw position
    public bool VerifyPlacement(TransformComponent xform)
    {
        var spaceDistance = 3;
        var worldPos = _transformSystem.GetWorldPosition(xform);
        var pos = xform.LocalPosition + new Vector2(0, 1f);
        var box = new Box2(pos + new Vector2(-1.4f, -0.4f), pos + new Vector2(1.4f, 0.4f));

        // MAKE SURE WE'RE STANDING ON A GRID
        if (!_entityManager.TryGetComponent<MapGridComponent>(xform.GridUid, out var grid))
        {
            return false;
        }

        // CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        foreach (var tile in _mapSystem.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (!tile.IsSpace(_tileDef))
                continue;
            return false;
        }

        // cannot do this check clientside todo fix this? not sure if that's even possible
        // CHECK IF WE'RE ON THE STATION OR IF SOMEONE'S TRYING TO SNEAK THIS ONTO SOMETHING SMOL
        //var station = _station.GetStationInMap(xform.MapID);
        //EntityUid? stationGrid = null;

        //if (!_entityManager.TryGetComponent<StationDataComponent>(station, out var stationData))
        //    stationGrid = _station.GetLargestGrid(stationData);

        //if (stationGrid is not null && stationGrid != xform.GridUid)
        //{
        //    return false;
        //}

        // CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
        if (_lookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, _playerManager.LocalEntity))
        {
            return false;
        }

        //if all of those aren't false, return true
        return true;
    }
}
