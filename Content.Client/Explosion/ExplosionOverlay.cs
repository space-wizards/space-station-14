using Content.Shared.Explosion;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Explosion;

[UsedImplicitly]
public sealed class ExplosionOverlay : Overlay
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private ShaderInstance _shader;

    public ExplosionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var drawHandle = args.WorldHandle;
        drawHandle.UseShader(_shader);

        var xforms = _entMan.GetEntityQuery<TransformComponent>();

        foreach (var (comp, appearance) in _entMan.EntityQuery<ExplosionVisualsComponent, AppearanceComponent>(true))
        {
            if (comp.Epicenter.MapId != args.MapId)
                continue;

            if (!appearance.TryGetData(ExplosionAppearanceData.Progress, out int index))
                continue;

            index = Math.Min(index, comp.Intensity.Count - 1);
            DrawExplosion(drawHandle, args.WorldBounds, comp, index, xforms);
        }

        drawHandle.SetTransform(Matrix3.Identity);
        drawHandle.UseShader(null);
    }

    private void DrawExplosion(DrawingHandleWorld drawHandle, Box2Rotated worldBounds, ExplosionVisualsComponent exp, int index, EntityQuery<TransformComponent> xforms)
    {
        Box2 gridBounds;
        foreach (var (gridId, tiles) in exp.Tiles)
        {
            if (!_mapManager.TryGetGrid(gridId, out var grid))
                continue;

            var xform = xforms.GetComponent(grid.Owner);
            var (_, _, worldMatrix, invWorldMatrix) = xform.GetWorldPositionRotationMatrixWithInv(xforms);

            gridBounds = invWorldMatrix.TransformBox(worldBounds).Enlarged(grid.TileSize * 2);
            drawHandle.SetTransform(worldMatrix);

            DrawTiles(drawHandle, gridBounds, index, tiles, exp, grid.TileSize);
        }

        if (exp.SpaceTiles == null)
            return;

        gridBounds = Matrix3.Invert(exp.SpaceMatrix).TransformBox(worldBounds).Enlarged(2);
        drawHandle.SetTransform(exp.SpaceMatrix);

        DrawTiles(drawHandle, gridBounds, index, exp.SpaceTiles, exp, exp.SpaceTileSize);
    }

    private void DrawTiles(
        DrawingHandleWorld drawHandle,
        Box2 gridBounds,
        int index,
        Dictionary<int, List<Vector2i>> tileSets,
        ExplosionVisualsComponent exp,
        ushort tileSize)
    {
        for (var j = 0; j <= index; j++)
        {
            if (!tileSets.TryGetValue(j, out var tiles))
                continue;

            var frameIndex = (int) Math.Min(exp.Intensity[j] / exp.IntensityPerState, exp.FireFrames.Count - 1);
            var frames = exp.FireFrames[frameIndex];

            foreach (var tile in tiles)
            {
                Vector2 centre = ((Vector2) tile + 0.5f) * tileSize;

                if (!gridBounds.Contains(centre))
                    continue;

                var texture = _robustRandom.Pick(frames);
                drawHandle.DrawTextureRect(texture, Box2.CenteredAround(centre, (tileSize, tileSize)), exp.FireColor);
            }
        }
    }
}
