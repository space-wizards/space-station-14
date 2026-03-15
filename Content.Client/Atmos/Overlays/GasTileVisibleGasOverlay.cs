using Content.Client.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Numerics;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Atmos.Overlays;

/// <summary>
/// Overlay responsible for rendering visible atmos gasses (like plasma for example) usin.
/// </summary>
public sealed class GasTileVisibleGasOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private readonly SharedAtmosphereSystem _atmosphereSystem;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _xformSys;
    private readonly SharedGasTileOverlaySystem _gasTileOverlaySystem;
    private readonly SpriteSystem _spriteSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities | OverlaySpace.WorldSpaceBelowWorld;
    private readonly ShaderInstance _shader;

    // Gas overlays
    private readonly float[] _timer;
    private readonly float[][] _frameDelays;
    private readonly int[] _frameCounter;

    // TODO combine textures into a single texture atlas.
    private readonly Texture[][] _frames;

    private readonly int _gasCount;
    private int _smoothingSubdivisionsPerAxis;

    public const int GasOverlayZIndex = (int)DrawDepth.Gasses; // Under ghosts and fire, above mostly everything else

    public GasTileVisibleGasOverlay()
    {
        IoCManager.InjectDependencies(this);
        _atmosphereSystem = _entManager.System<SharedAtmosphereSystem>();
        _mapSystem = _entManager.System<SharedMapSystem>();
        _xformSys = _entManager.System<SharedTransformSystem>();
        _gasTileOverlaySystem = _entManager.System<SharedGasTileOverlaySystem>();
        _spriteSystem = _entManager.System<SpriteSystem>();

        _shader = _protoManager.Index(UnshadedShader).Instance();
        ZIndex = GasOverlayZIndex;

        _gasCount = _gasTileOverlaySystem.VisibleGasId.Length;
        _timer = new float[_gasCount];
        _frameDelays = new float[_gasCount][];
        _frameCounter = new int[_gasCount];
        _frames = new Texture[_gasCount][];

        for (var i = 0; i < _gasCount; i++)
        {
            var gasPrototype = _atmosphereSystem.GetGas(_gasTileOverlaySystem.VisibleGasId[i]);

            SpriteSpecifier overlay;

            if (!string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) &&
                !string.IsNullOrEmpty(gasPrototype.GasOverlayState))
                overlay = new SpriteSpecifier.Rsi(new(gasPrototype.GasOverlaySprite), gasPrototype.GasOverlayState);
            else if (!string.IsNullOrEmpty(gasPrototype.GasOverlayTexture))
                overlay = new SpriteSpecifier.Texture(new(gasPrototype.GasOverlayTexture));
            else
                continue;

            switch (overlay)
            {
                case SpriteSpecifier.Rsi animated:
                    var rsi = _resourceCache.GetResource<RSIResource>(animated.RsiPath).RSI;
                    var stateId = animated.RsiState;

                    if (!rsi.TryGetState(stateId, out var state))
                        continue;

                    _frames[i] = state.GetFrames(RsiDirection.South);
                    _frameDelays[i] = state.GetDelays();
                    _frameCounter[i] = 0;
                    break;
                case SpriteSpecifier.Texture texture:
                    _frames[i] = new[] { _spriteSystem.Frame0(texture) };
                    _frameDelays[i] = Array.Empty<float>();
                    break;
            }
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        for (var i = 0; i < _gasCount; i++)
        {
            var delays = _frameDelays[i];
            if (delays.Length == 0)
                continue;

            var frameCount = _frameCounter[i];
            _timer[i] += args.DeltaSeconds;
            var time = delays[frameCount];

            if (_timer[i] < time)
                continue;

            _timer[i] -= time;
            _frameCounter[i] = (frameCount + 1) % _frames[i].Length;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var drawHandle = args.WorldHandle;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();
        var gridState = (args.WorldBounds,
            args.WorldHandle,
            _gasCount,
            _smoothingSubdivisionsPerAxis,
            _frames,
            _frameCounter,
            _shader,
            overlayQuery,
            xformQuery,
            _xformSys);

        var mapUid = _mapSystem.GetMapOrInvalid(args.MapId);

        if (_entManager.TryGetComponent<MapAtmosphereComponent>(mapUid, out var atmos))
            DrawMapOverlay(drawHandle, args, mapUid, atmos);

        if (args.Space != OverlaySpace.WorldSpaceEntities)
            return;

        // TODO: WorldBounds callback.
        _mapManager.FindGridsIntersecting(args.MapId,
            args.WorldAABB,
            ref gridState,
            static (EntityUid uid,
                MapGridComponent grid,
                ref (Box2Rotated WorldBounds,
                    DrawingHandleWorld drawHandle,
                    int gasCount,
                    int smoothingSubdivisionsPerAxis,
                    Texture[][] frames,
                    int[] frameCounter,
                    ShaderInstance shader,
                    EntityQuery<GasTileOverlayComponent> overlayQuery,
                    EntityQuery<TransformComponent> xformQuery,
                    SharedTransformSystem xformSys) state) =>
            {
                if (!state.overlayQuery.TryGetComponent(uid, out var comp) ||
                    !state.xformQuery.TryGetComponent(uid, out var gridXform))
                {
                    return true;
                }

                var (_, _, worldMatrix, invMatrix) = state.xformSys.GetWorldPositionRotationMatrixWithInv(gridXform);
                state.drawHandle.SetTransform(worldMatrix);
                var floatBounds = invMatrix.TransformBox(state.WorldBounds).Enlarged(grid.TileSize);
                var localBounds = new Box2i(
                    (int)MathF.Floor(floatBounds.Left),
                    (int)MathF.Floor(floatBounds.Bottom),
                    (int)MathF.Ceiling(floatBounds.Right),
                    (int)MathF.Ceiling(floatBounds.Top));

                // Currently it would be faster to group drawing by gas rather than by chunk, but if the textures are
                // ever moved to a single atlas, that should no longer be the case. So this is just grouping draw calls
                // by chunk, even though its currently slower.

                state.drawHandle.UseShader(null);
                foreach (var chunk in comp.Chunks.Values)
                {
                    var enumerator = new GasChunkEnumerator(chunk);

                    while (enumerator.MoveNext(out var gas))
                    {
                        if (gas.Opacity == null!)
                            continue;

                        var tilePosition = chunk.Origin + (enumerator.X, enumerator.Y);
                        if (!localBounds.Contains(tilePosition))
                            continue;

                        for (var i = 0; i < state.gasCount; i++)
                        {
                            var opacity = gas.Opacity[i];

                            if (opacity == 0)
                                continue;

                            DrawSmoothedGasTile(
                                state.drawHandle,
                                state.frames[i][state.frameCounter[i]],
                                tilePosition,
                                i,
                                opacity,
                                comp.Chunks,
                                state.smoothingSubdivisionsPerAxis);
                        }
                    }
                }

                return true;
            });

        drawHandle.UseShader(null);
        drawHandle.SetTransform(Matrix3x2.Identity);
    }

    /// <summary>
    /// Controls how many discrete smoothing segments are rendered per tile axis.
    /// </summary>
    public void SetSmoothingSubdivisionsPerAxis(int value)
    {
        _smoothingSubdivisionsPerAxis = Math.Clamp(value, 1, 32);
    }

    /// <summary>
    /// Draws a gas overlay tile with edge smoothing based on neighbouring gas opacity.
    /// </summary>
    /// <remarks>
    /// The function renders a gas tile whose opacity smoothly blends with adjacent tiles.
    /// It samples the opacity of the four cardinal neighbours (N,S,E,W) and four diagonal neighbours
    /// to approximate how the gas density changes across the tile.
    /// </remarks>
    private static void DrawSmoothedGasTile(
        DrawingHandleWorld drawHandle,
        Texture texture,
        Vector2i tileIndices,
        int gasIndex,
        byte centerOpacity,
        Dictionary<Vector2i, GasOverlayChunk> chunks,
        int smoothingSubdivisionsPerAxis)
    {
        var centerChunkIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(tileIndices);
        if (!chunks.TryGetValue(centerChunkIndices, out var centerChunk))
            return;

        var centerLocalX = tileIndices.X - centerChunk.Origin.X;
        var centerLocalY = tileIndices.Y - centerChunk.Origin.Y;
        if ((uint)centerLocalX >= SharedGasTileOverlaySystem.ChunkSize ||
            (uint)centerLocalY >= SharedGasTileOverlaySystem.ChunkSize)
        {
            return;
        }

        var centerData = centerChunk.TileData[centerLocalX + centerLocalY * SharedGasTileOverlaySystem.ChunkSize];
        if (centerData.ByteGasTemperature.IsAtmosImpossible)
            return;

        byte GetOpacity(Vector2i indices)
        {
            var localX = indices.X - centerChunk.Origin.X;
            var localY = indices.Y - centerChunk.Origin.Y;
            if ((uint)localX < SharedGasTileOverlaySystem.ChunkSize &&
                (uint)localY < SharedGasTileOverlaySystem.ChunkSize)
            {
                var data = centerChunk.TileData[localX + localY * SharedGasTileOverlaySystem.ChunkSize];
                if (data.ByteGasTemperature.IsAtmosImpossible)
                    return centerOpacity;

                return gasIndex < data.Opacity.Length ? data.Opacity[gasIndex] : (byte)0;
            }

            if (!SharedGasTileOverlaySystem.TryGetOverlayData(chunks, indices, out var data2))
                return 0;

            if (data2.ByteGasTemperature.IsAtmosImpossible)
                return centerOpacity;

            return gasIndex < data2.Opacity.Length ? data2.Opacity[gasIndex] : (byte)0;
        }

        var n = GetOpacity(tileIndices + Vector2i.Up);
        var s = GetOpacity(tileIndices + Vector2i.Down);
        var e = GetOpacity(tileIndices + Vector2i.Right);
        var w = GetOpacity(tileIndices + Vector2i.Left);

        // Dense clouds are common and can use the old single draw call path.
        if (n == centerOpacity && s == centerOpacity && e == centerOpacity && w == centerOpacity)
        {
            drawHandle.DrawTexture(texture, tileIndices, Color.White.WithAlpha(centerOpacity));
            return;
        }

        var nw = GetOpacity(tileIndices + Vector2i.UpLeft);
        var ne = GetOpacity(tileIndices + Vector2i.UpRight);
        var sw = GetOpacity(tileIndices + Vector2i.DownLeft);
        var se = GetOpacity(tileIndices + Vector2i.DownRight);

        var alphaNw = BlendCorner(centerOpacity, n, w, nw);
        var alphaNe = BlendCorner(centerOpacity, n, e, ne);
        var alphaSw = BlendCorner(centerOpacity, s, w, sw);
        var alphaSe = BlendCorner(centerOpacity, s, e, se);

        if (alphaNw == centerOpacity && alphaNe == centerOpacity && alphaSw == centerOpacity && alphaSe == centerOpacity)
        {
            drawHandle.DrawTexture(texture, tileIndices, Color.White.WithAlpha(centerOpacity));
            return;
        }

        if (smoothingSubdivisionsPerAxis <= 1)
        {
            var uniformAlpha = (byte)((alphaNw + alphaNe + alphaSw + alphaSe + 2) / 4);
            if (uniformAlpha > 0)
                drawHandle.DrawTexture(texture, tileIndices, Color.White.WithAlpha(uniformAlpha));
            return;
        }

        var texWidth = texture.Width;
        var texHeight = texture.Height;
        var x = tileIndices.X;
        var y = tileIndices.Y;
        var segmentWorldSize = 1f / smoothingSubdivisionsPerAxis;
        var segmentTextureWidth = (float)texWidth / smoothingSubdivisionsPerAxis;
        var segmentTextureHeight = (float)texHeight / smoothingSubdivisionsPerAxis;

        for (var sx = 0; sx < smoothingSubdivisionsPerAxis; sx++)
        {
            var sampleX = (sx + 0.5f) / smoothingSubdivisionsPerAxis;

            for (var sy = 0; sy < smoothingSubdivisionsPerAxis; sy++)
            {
                var sampleY = (sy + 0.5f) / smoothingSubdivisionsPerAxis;
                var alpha = Bilinear(sampleX, sampleY, alphaSw, alphaSe, alphaNw, alphaNe);
                if (alpha == 0)
                    continue;

                var worldRect = Box2.FromDimensions(
                    x + sx * segmentWorldSize,
                    y + sy * segmentWorldSize,
                    segmentWorldSize,
                    segmentWorldSize);

                // World-space Y grows upwards, sub-region Y grows downwards.
                var subTop = texHeight - (sy + 1) * segmentTextureHeight;
                var subBottom = texHeight - sy * segmentTextureHeight;
                var subLeft = sx * segmentTextureWidth;
                var subRight = (sx + 1) * segmentTextureWidth;

                drawHandle.DrawTextureRectRegion(
                    texture,
                    worldRect,
                    Color.White.WithAlpha(alpha),
                    new UIBox2(subLeft, subTop, subRight, subBottom));
            }
        }
    }

    private static byte BlendCorner(byte center, byte cardA, byte cardB, byte diagonal)
    {
        // 4:2:2:1 (center:cardinal:cardinal:diagonal) keeps edges soft without washing out dense tiles.
        const int blendCenterWeight = 4;
        const int blendCardinalWeight = 2;
        const int blendDiagonalWeight = 1;
        const int blendWeightTotal = blendCenterWeight + blendCardinalWeight + blendCardinalWeight + blendDiagonalWeight;

        var weighted = center * blendCenterWeight + cardA * blendCardinalWeight + cardB * blendCardinalWeight + diagonal * blendDiagonalWeight;
        return (byte)((weighted + blendWeightTotal / 2) / blendWeightTotal);
    }

    private static byte Bilinear(float x, float y, byte sw, byte se, byte nw, byte ne)
    {
        var south = MathHelper.Lerp(sw, se, x);
        var north = MathHelper.Lerp(nw, ne, x);
        return (byte)MathF.Round(MathHelper.Lerp(south, north, y));
    }

    private void DrawMapOverlay(
        DrawingHandleWorld handle,
        OverlayDrawArgs args,
        EntityUid map,
        MapAtmosphereComponent atmos)
    {
        var mapGrid = _entManager.HasComponent<MapGridComponent>(map);

        // map-grid atmospheres get drawn above grids
        if (mapGrid && args.Space != OverlaySpace.WorldSpaceEntities)
            return;

        // Normal map atmospheres get drawn below grids
        if (!mapGrid && args.Space != OverlaySpace.WorldSpaceBelowWorld)
            return;

        var bottomLeft = args.WorldAABB.BottomLeft.Floored();
        var topRight = args.WorldAABB.TopRight.Ceiled();

        for (var x = bottomLeft.X; x <= topRight.X; x++)
        {
            for (var y = bottomLeft.Y; y <= topRight.Y; y++)
            {
                var tilePosition = new Vector2(x, y);

                for (var i = 0; i < atmos.OverlayData.Opacity.Length; i++)
                {
                    var opacity = atmos.OverlayData.Opacity[i];

                    if (opacity > 0)
                        handle.DrawTexture(_frames[i][_frameCounter[i]], tilePosition, Color.White.WithAlpha(opacity));
                }
            }
        }
    }
}
