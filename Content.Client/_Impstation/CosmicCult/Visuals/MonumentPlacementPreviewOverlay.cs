using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Impstation.CosmicCult.Visuals;

public sealed class MonumentPlacementPreviewOverlay : Overlay
{
    private readonly IEntityManager _entityManager;
    private readonly IPlayerManager _playerManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly SharedMapSystem _mapSystem;
    private readonly MonumentPlacementPreviewSystem _preview;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly ShaderInstance _shader;
    public bool LockPlacement = false;
    private Vector2 _lastPos = Vector2.Zero;

    //evil huge ctor because doing iocmanager stuff was killing the client for some reason
    public MonumentPlacementPreviewOverlay(IEntityManager entityManager, IPlayerManager playerManager, SpriteSystem spriteSystem, SharedMapSystem mapSystem, IPrototypeManager protoMan, MonumentPlacementPreviewSystem preview)
    {
        _entityManager = entityManager;
        _playerManager = playerManager;
        _spriteSystem = spriteSystem;
        _mapSystem = mapSystem;
        _shader = protoMan.Index<ShaderPrototype>("unshaded").Instance();
        _preview = preview;

        ZIndex = (int) Shared.DrawDepth.DrawDepth.Mobs; //make the overlay render at the same depth as the actual sprite. might want to make it 1 lower if things get wierd with it.
    }

    //this might get wierd if the player managed to leave the grid they put the monument on? theoretically not a concern because it can't be placed too close to space.
    //shouldn't crash due to the comp checks, though.
    //todo make the overlay fade in / out? that's for the ensaucening later though
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(_playerManager.LocalEntity, out var transformComp))
            return;

        if (!_entityManager.TryGetComponent<MapGridComponent>(transformComp.GridUid, out var grid))
            return;

        if (!_entityManager.TryGetComponent<TransformComponent>(transformComp.ParentUid, out var parentTransform))
            return;

        //todo make this get passed in from somewhere else?
        //and / or make it not use the raw path but I hate RSIs with a probably unhealthy passion
        var tex = new SpriteSpecifier.Texture(new ("_Impstation/CosmicCult/Tileset/monument.rsi/stage1.png"));

        var worldHandle = args.WorldHandle;

        //stuff to make the monument preview stick in place once the monument
        Color color;
        if (!LockPlacement)
        {
            //snap the preview to the tile we'll be spawning the monument on
            var localTile = _mapSystem.GetTileRef(transformComp.GridUid.Value, grid, transformComp.Coordinates);
            var targetIndices = localTile.GridIndices + new Vector2i(0, 1);
            var snappedCoords = _mapSystem.ToCenterCoordinates(transformComp.GridUid.Value, targetIndices, grid);
            _lastPos = snappedCoords.Position; //update the position

            //set the colour based on if the target tile is valid or not todo make this something else? like a toggle in a shader or so? that's for later anyway
            color = _preview.VerifyPlacement(transformComp) ? Color.Green : Color.Red;
        }
        else
        {
            //if the position is locked, then it has to be valid so always use green
            color = Color.Green;
        }

        worldHandle.SetTransform(parentTransform.LocalMatrix);
        worldHandle.UseShader(_shader);
        worldHandle.DrawTexture(_spriteSystem.Frame0(tex), _lastPos - new Vector2(1.5f, 0.5f), color); //needs the offset to render in the proper position
        worldHandle.UseShader(null);
    }

    //copied out from the ability code todo update this & the ability to check the snapped position instead of the raw position
    //this might be slightly desynced from the ability detection code?
    /*
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
    */
}
