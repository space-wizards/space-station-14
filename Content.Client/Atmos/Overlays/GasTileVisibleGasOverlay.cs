using Content.Client.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
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
public sealed partial class GasTileVisibleGasOverlay : Overlay
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private readonly SharedAtmosphereSystem _atmosphereSystem;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _xformSys;
    private readonly SharedGasTileOverlaySystem _gasTileOverlaySystem;
    private readonly ChunkEntitySystem _chunkEntitySystem;
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

    public const int GasOverlayZIndex = (int)DrawDepth.Gasses; // Under ghosts and fire, above mostly everything else

    public GasTileVisibleGasOverlay()
    {
        IoCManager.InjectDependencies(this);
        _atmosphereSystem = _entManager.System<SharedAtmosphereSystem>();
        _mapSystem = _entManager.System<SharedMapSystem>();
        _xformSys = _entManager.System<SharedTransformSystem>();
        _gasTileOverlaySystem = _entManager.System<SharedGasTileOverlaySystem>();
        _chunkEntitySystem = _entManager.System<ChunkEntitySystem>();
        _spriteSystem = _entManager.System<SpriteSystem>();

        _shader = _protoManager.Index(UnshadedShader).Instance();
        ZIndex = GasOverlayZIndex;

        _gasCount = _gasTileOverlaySystem.VisibleGasCount;
        _timer = new float[_gasCount];
        _frameDelays = new float[_gasCount][];
        _frameCounter = new int[_gasCount];
        _frames = new Texture[_gasCount][];

        var visibleGasIndex = 0;
        for (var gasId = 0; gasId < Atmospherics.TotalNumberOfGases; gasId++)
        {
            if (!_gasTileOverlaySystem.IsGasVisible(gasId))
                continue;

            var gasPrototype = _atmosphereSystem.GetGas(gasId);

            switch (gasPrototype.GasOverlaySprite)
            {
                case SpriteSpecifier.Rsi animated:
                    var rsi = _resourceCache.GetResource<RSIResource>(animated.RsiPath).RSI;
                    var stateId = animated.RsiState;

                    if (!rsi.TryGetState(stateId, out var state))
                    {
                        visibleGasIndex++;
                        continue;
                    }

                    _frames[visibleGasIndex] = state.GetFrames(RsiDirection.South);
                    _frameDelays[visibleGasIndex] = state.GetDelays();
                    _frameCounter[visibleGasIndex] = 0;
                    break;
                case SpriteSpecifier.Texture texture:
                    _frames[visibleGasIndex] = new[] { _spriteSystem.Frame0(texture) };
                    _frameDelays[visibleGasIndex] = Array.Empty<float>();
                    break;
            }

            visibleGasIndex++;
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
        var overlayQuery = _entManager.GetEntityQuery<GasOverlayChunkComponent>();
        var gridState = (args.WorldBounds,
            args.WorldHandle,
            _gasCount,
            _frames,
            _frameCounter,
            _shader,
            _chunkEntitySystem,
            overlayQuery,
            xformQuery,
            _xformSys);

        var mapUid = _mapSystem.GetMapOrInvalid(args.MapId);

        if (_entManager.TryGetComponent<MapAtmosphereComponent>(mapUid, out var atmos))
            DrawMapOverlay(drawHandle, args, mapUid, atmos);

        if (args.Space != OverlaySpace.WorldSpaceEntities)
            return;

        // TODO: WorldBounds callback.
        _mapSystem.FindGridsIntersecting(args.MapId,
            args.WorldAABB,
            ref gridState,
            static (EntityUid uid,
                MapGridComponent grid,
                ref (Box2Rotated WorldBounds,
                    DrawingHandleWorld drawHandle,
                    int gasCount,
                    Texture[][] frames,
                    int[] frameCounter,
                    ShaderInstance shader,
                    ChunkEntitySystem chunkEntitySystem,
                    EntityQuery<GasOverlayChunkComponent> overlayQuery,
                    EntityQuery<TransformComponent> xformQuery,
                    SharedTransformSystem xformSys) state) =>
            {
                if (!state.xformQuery.TryGetComponent(uid, out var gridXform))
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
                var chunks = state.chunkEntitySystem.GetChunksIntersecting(uid, floatBounds, state.overlayQuery);
                while (chunks.MoveNext(out var chunkEnt))
                {
                    var chunk = chunkEnt.Value.Comp2;
                    var chunkOrigin = chunkEnt.Value.Comp1.Chunk * SharedGasTileOverlaySystem.ChunkSize;
                    var enumerator = new GasChunkEnumerator(chunk);

                    while (enumerator.MoveNext(out var gas))
                    {
                        if (gas.PackedOpacity == 0)
                            continue;

                        var tilePosition = chunkOrigin + (enumerator.X, enumerator.Y);
                        if (!localBounds.Contains(tilePosition))
                            continue;

                        for (var i = 0; i < state.gasCount; i++)
                        {
                            var opacity = gas.GetOpacity(i);
                            if (opacity > 0)
                            {
                                state.drawHandle.DrawTexture(state.frames[i][state.frameCounter[i]],
                                    tilePosition,
                                    Color.White.WithAlpha(opacity));
                            }
                        }
                    }
                }

                return true;
            });

        drawHandle.UseShader(null);
        drawHandle.SetTransform(Matrix3x2.Identity);
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

                for (var i = 0; i < _gasCount; i++)
                {
                    var opacity = atmos.OverlayData.GetOpacity(i);

                    if (opacity > 0)
                        handle.DrawTexture(_frames[i][_frameCounter[i]], tilePosition, Color.White.WithAlpha(opacity));
                }
            }
        }
    }
}
