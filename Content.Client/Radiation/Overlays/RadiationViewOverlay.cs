using Content.Client.Radiation.Systems;
using Content.Shared.Radiation.Systems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Radiation.Overlays;

public sealed class RadiationViewOverlay : Overlay
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    private readonly Font _font;


    public RadiationViewOverlay()
    {
        IoCManager.InjectDependencies(this);

        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var radSys = EntitySystem.Get<RadiationSystem>();

        //if (radSys.MapId != args.Viewport.Eye?.Position.MapId)
          //  return;

        switch (args.Space)
        {
            case OverlaySpace.ScreenSpace:
                DrawScreen(args);
                break;
            case OverlaySpace.WorldSpace:
                DrawWorld(args);
                break;
        }
    }

    private void DrawScreen(OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        Box2 gridBounds;
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        var radSys = EntitySystem.Get<RadiationSystem>();

        foreach (var (gridUid, resGrid) in radSys._radiationMap)
        {
            if (!_mapManager.TryGetGrid(gridUid, out var grid))
                continue;

            var gridXform = xformQuery.GetComponent(grid.GridEntityId);
            var (_, _, matrix, invMatrix) = gridXform.GetWorldPositionRotationMatrixWithInv(xformQuery);
            gridBounds = invMatrix.TransformBox(args.WorldBounds);
            DrawText(handle, gridBounds, matrix, resGrid, Color.White, grid.TileSize);
        }

        foreach (var (gridUid, resGrid) in radSys._resistancePerTile)
        {
            if (!_mapManager.TryGetGrid(gridUid, out var grid))
                continue;

            var gridXform = xformQuery.GetComponent(grid.GridEntityId);
            var (_, _, matrix, invMatrix) = gridXform.GetWorldPositionRotationMatrixWithInv(xformQuery);
            gridBounds = invMatrix.TransformBox(args.WorldBounds);
            DrawText(handle, gridBounds, matrix, resGrid, Color.Gray, grid.TileSize, 0.25f);
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        var radSys = EntitySystem.Get<RadiationSystem>();

        foreach (var (gridUid, map) in radSys._radiationMap)
        {
            if (!_mapManager.TryGetGrid(gridUid, out var grid))
                continue;

            var handle = args.WorldHandle;

            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            var gridXform = xformQuery.GetComponent(grid.GridEntityId);
            var (_, _, worldMatrix, invWorldMatrix) = gridXform.GetWorldPositionRotationMatrixWithInv(xformQuery);

            var gridBounds = invWorldMatrix.TransformBox(args.WorldBounds);
            handle.SetTransform(worldMatrix);
            DrawTiles(handle, gridBounds, map);
        }
    }

    private void DrawText(DrawingHandleScreen handle, Box2 gridBounds,
        Matrix3 transform, Dictionary<Vector2i, float> tiles, Color color,
        ushort tileSize = 1, float margin = 0.5f)
    {
        foreach (var (tile, rad) in tiles)
        {
            var centre = ((Vector2) tile + margin) * tileSize;

            // is the center of this tile visible to the user?
            if (!gridBounds.Contains(centre))
                continue;

            var worldCenter = transform.Transform(centre);

            var screenCenter = _eyeManager.WorldToScreen(worldCenter);

            if (rad > 9)
                screenCenter += (-12, -8);
            else
                screenCenter += (-8, -8);

            handle.DrawString(_font, screenCenter, rad.ToString("F2"), color);
        }
    }

    private void DrawTiles(DrawingHandleWorld handle, Box2 gridBounds,
        Dictionary<Vector2i, float> tiles, ushort tileSize = 1)
    {
        var color = Color.Green;
        color.A = 0.2f;

        foreach (var (tile, rad) in tiles)
        {
            var centre = ((Vector2) tile + 0.5f) * tileSize;

            // is the center of this tile visible to the user?
            if (!gridBounds.Contains(centre))
                continue;

            var box = Box2.UnitCentered.Translated(centre);
            handle.DrawRect(box, color);
        }
    }
}
