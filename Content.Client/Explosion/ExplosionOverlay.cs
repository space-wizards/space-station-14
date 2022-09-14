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
    /// <summary>
    ///     The explosion that needs to be drawn. This explosion is currently being processed by the server and
    ///     expanding outwards.
    /// </summary>
    internal Explosion? ActiveExplosion;

    /// <summary>
    ///     This index specifies what parts of the currently expanding explosion should be drawn.
    /// </summary>
    public int Index;

    /// <summary>
    ///     These explosions have finished expanding, but we will draw for a few more frames. This is important for
    ///     small explosions, as otherwise they disappear far too quickly.
    /// </summary>
    internal List<Explosion> CompletedExplosions = new ();

    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private ShaderInstance _shader;

    public ExplosionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var drawHandle = args.WorldHandle;
        drawHandle.UseShader(_shader);

        var xforms = _entMan.GetEntityQuery<TransformComponent>();

        if (ActiveExplosion != null && ActiveExplosion.Map == args.Viewport.Eye?.Position.MapId)
        {
            DrawExplosion(drawHandle, args.WorldBounds, ActiveExplosion, Index, xforms);
        }

        foreach (var exp in CompletedExplosions)
        {
            if (exp.Map == args.Viewport.Eye?.Position.MapId)
                DrawExplosion(drawHandle, args.WorldBounds, exp, exp.Intensity.Count, xforms);
        }

        drawHandle.SetTransform(Matrix3.Identity);
        drawHandle.UseShader(null);
    }

    private void DrawExplosion(DrawingHandleWorld drawHandle, Box2Rotated worldBounds, Explosion exp, int index, EntityQuery<TransformComponent> xforms)
    {
        Box2 gridBounds;
        foreach (var (gridId, tiles) in exp.Tiles)
        {
            if (!_mapManager.TryGetGrid(gridId, out var grid))
                continue;

            var xform = xforms.GetComponent(grid.GridEntityId);
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
        Explosion exp,
        ushort tileSize)
    {
        for (var j = 0; j < index; j++)
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
