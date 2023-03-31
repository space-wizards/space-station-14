using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Client.Administration.UI.SpawnExplosion;

[UsedImplicitly]
public sealed class ExplosionDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public Dictionary<int, List<Vector2i>>? SpaceTiles;
    public Dictionary<EntityUid, Dictionary<int, List<Vector2i>>> Tiles = new();
    public List<float> Intensity = new();
    public float TotalIntensity;
    public float Slope;
    public ushort SpaceTileSize;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    public Matrix3 SpaceMatrix;
    public MapId Map;

    private readonly Font _font;

    public ExplosionDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Map != args.Viewport.Eye?.Position.MapId)
            return;

        if (Tiles.Count == 0 && SpaceTiles == null)
            return;

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

        foreach (var (gridId, tileSets) in Tiles)
        {
            if (!_mapManager.TryGetGrid(gridId, out var grid))
                continue;

            var gridXform = xformQuery.GetComponent(grid.Owner);
            var (_, _, matrix, invMatrix) = gridXform.GetWorldPositionRotationMatrixWithInv(xformQuery);
            gridBounds = invMatrix.TransformBox(args.WorldBounds).Enlarged(grid.TileSize * 2);
            DrawText(handle, gridBounds, matrix, tileSets, grid.TileSize);
        }

        if (SpaceTiles == null)
            return;

        gridBounds = Matrix3.Invert(SpaceMatrix).TransformBox(args.WorldBounds);

        DrawText(handle, gridBounds, SpaceMatrix, SpaceTiles, SpaceTileSize);
    }

    private void DrawText(
        DrawingHandleScreen handle,
        Box2 gridBounds,
        Matrix3 transform,
        Dictionary<int, List<Vector2i>> tileSets,
        ushort tileSize)
    {
        for (var i = 1; i < Intensity.Count; i++)
        {
            if (!tileSets.TryGetValue(i, out var tiles))
                continue;

            foreach (var tile in tiles)
            {
                var centre = ((Vector2) tile + 0.5f) * tileSize;

                // is the center of this tile visible to the user?
                if (!gridBounds.Contains(centre))
                    continue;

                var worldCenter = transform.Transform(centre);

                var screenCenter = _eyeManager.WorldToScreen(worldCenter);

                if (Intensity[i] > 9)
                    screenCenter += (-12, -8);
                else
                    screenCenter += (-8, -8);

                handle.DrawString(_font, screenCenter, Intensity[i].ToString("F2"));
            }
        }

        if (tileSets.ContainsKey(0))
        {
            var epicenter = tileSets[0].First();
            var worldCenter = transform.Transform(((Vector2) epicenter + 0.5f) * tileSize);
            var screenCenter = _eyeManager.WorldToScreen(worldCenter) + (-24, -24);
            var text = $"{Intensity[0]:F2}\nΣ={TotalIntensity:F1}\nΔ={Slope:F1}";
            handle.DrawString(_font, screenCenter, text);
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        Box2 gridBounds;
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

        foreach (var (gridId, tileSets) in Tiles)
        {
            if (!_mapManager.TryGetGrid(gridId, out var grid))
                continue;

            var gridXform = xformQuery.GetComponent(grid.Owner);
            var (_, _, worldMatrix, invWorldMatrix) = gridXform.GetWorldPositionRotationMatrixWithInv(xformQuery);
            gridBounds = invWorldMatrix.TransformBox(args.WorldBounds).Enlarged(grid.TileSize * 2);
            handle.SetTransform(worldMatrix);
            DrawTiles(handle, gridBounds, tileSets, SpaceTileSize);
        }

        if (SpaceTiles == null)
            return;

        gridBounds = Matrix3.Invert(SpaceMatrix).TransformBox(args.WorldBounds).Enlarged(2);
        handle.SetTransform(SpaceMatrix);

        DrawTiles(handle, gridBounds, SpaceTiles, SpaceTileSize);
        handle.SetTransform(Matrix3.Identity);
    }

    private void DrawTiles(
        DrawingHandleWorld handle,
        Box2 gridBounds,
        Dictionary<int, List<Vector2i>> tileSets,
        ushort tileSize)
    {
        for (var i = 0; i < Intensity.Count; i++)
        {
            var color = ColorMap(Intensity[i]);
            var colorTransparent = color;
            colorTransparent.A = 0.2f;

            if (!tileSets.TryGetValue(i, out var tiles))
                continue;

            foreach (var tile in tiles)
            {
                var centre = ((Vector2) tile + 0.5f) * tileSize;

                // is the center of this tile visible to the user?
                if (!gridBounds.Contains(centre))
                    continue;

                var box = Box2.UnitCentered.Translated(centre);
                handle.DrawRect(box, color, false);
                handle.DrawRect(box, colorTransparent);
            }
        }
    }

    private Color ColorMap(float intensity)
    {
        var frac = 1 - intensity / Intensity[0];
        Color result;
        if (frac < 0.5f)
            result = Color.InterpolateBetween(Color.Red, Color.Orange, frac * 2);
        else
            result = Color.InterpolateBetween(Color.Orange, Color.Yellow, (frac - 0.5f) * 2);
        return result;
    }
}
