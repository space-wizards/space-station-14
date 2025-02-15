using System.Numerics;
using Content.Client.Atmos.Components;
using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Prototypes;
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

namespace Content.Client.Atmos.Overlays
{
    public sealed class GasTileOverlay : Overlay
    {
        private readonly IEntityManager _entManager;
        private readonly IMapManager _mapManager;
        private readonly SharedTransformSystem _xformSys;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities | OverlaySpace.WorldSpaceBelowWorld;
        private readonly ShaderInstance _shader;

        // Gas overlays
        private readonly float[] _timer;
        private readonly float[][] _frameDelays;
        private readonly int[] _frameCounter;

        // TODO combine textures into a single texture atlas.
        private readonly Texture[][] _frames;

        // Fire overlays
        private const int FireStates = 3;
        private const string FireRsiPath = "/Textures/Effects/fire.rsi";

        private readonly float[] _fireTimer = new float[FireStates];
        private readonly float[][] _fireFrameDelays = new float[FireStates][];
        private readonly int[] _fireFrameCounter = new int[FireStates];
        private readonly Texture[][] _fireFrames = new Texture[FireStates][];

        private int _gasCount;

        public const int GasOverlayZIndex = (int) Shared.DrawDepth.DrawDepth.Effects; // Under ghosts, above mostly everything else

        public GasTileOverlay(GasTileOverlaySystem system, IEntityManager entManager, IResourceCache resourceCache, IPrototypeManager protoMan, SpriteSystem spriteSys, SharedTransformSystem xformSys)
        {
            _entManager = entManager;
            _mapManager = IoCManager.Resolve<IMapManager>();
            _xformSys = xformSys;
            _shader = protoMan.Index<ShaderPrototype>("unshaded").Instance();
            ZIndex = GasOverlayZIndex;

            _gasCount = system.VisibleGasId.Length;
            _timer = new float[_gasCount];
            _frameDelays = new float[_gasCount][];
            _frameCounter = new int[_gasCount];
            _frames = new Texture[_gasCount][];

            for (var i = 0; i < _gasCount; i++)
            {
                var gasPrototype = protoMan.Index<GasPrototype>(system.VisibleGasId[i].ToString());

                SpriteSpecifier overlay;

                if (!string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) && !string.IsNullOrEmpty(gasPrototype.GasOverlayState))
                    overlay = new SpriteSpecifier.Rsi(new (gasPrototype.GasOverlaySprite), gasPrototype.GasOverlayState);
                else if (!string.IsNullOrEmpty(gasPrototype.GasOverlayTexture))
                    overlay = new SpriteSpecifier.Texture(new (gasPrototype.GasOverlayTexture));
                else
                    continue;

                switch (overlay)
                {
                    case SpriteSpecifier.Rsi animated:
                        var rsi = resourceCache.GetResource<RSIResource>(animated.RsiPath).RSI;
                        var stateId = animated.RsiState;

                        if (!rsi.TryGetState(stateId, out var state))
                            continue;

                        _frames[i] = state.GetFrames(RsiDirection.South);
                        _frameDelays[i] = state.GetDelays();
                        _frameCounter[i] = 0;
                        break;
                    case SpriteSpecifier.Texture texture:
                        _frames[i] = new[] { spriteSys.Frame0(texture) };
                        _frameDelays[i] = Array.Empty<float>();
                        break;
                }
            }

            var fire = resourceCache.GetResource<RSIResource>(FireRsiPath).RSI;

            for (var i = 0; i < FireStates; i++)
            {
                if (!fire.TryGetState((i + 1).ToString(), out var state))
                    throw new ArgumentOutOfRangeException($"Fire RSI doesn't have state \"{i}\"!");

                _fireFrames[i] = state.GetFrames(RsiDirection.South);
                _fireFrameDelays[i] = state.GetDelays();
                _fireFrameCounter[i] = 0;
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

            for (var i = 0; i < FireStates; i++)
            {
                var delays = _fireFrameDelays[i];
                if (delays.Length == 0)
                    continue;

                var frameCount = _fireFrameCounter[i];
                _fireTimer[i] += args.DeltaSeconds;
                var time = delays[frameCount];

                if (_fireTimer[i] < time) continue;
                _fireTimer[i] -= time;
                _fireFrameCounter[i] = (frameCount + 1) % _fireFrames[i].Length;
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
                _frames,
                _frameCounter,
                _fireFrames,
                _fireFrameCounter,
                _shader,
                overlayQuery,
                xformQuery,
                _xformSys);

            var mapUid = _mapManager.GetMapEntityId(args.MapId);

            if (_entManager.TryGetComponent<MapAtmosphereComponent>(mapUid, out var atmos))
                DrawMapOverlay(drawHandle, args, mapUid, atmos);

            if (args.Space != OverlaySpace.WorldSpaceEntities)
                return;

            // TODO: WorldBounds callback.
            _mapManager.FindGridsIntersecting(args.MapId, args.WorldAABB, ref gridState,
                static (EntityUid uid, MapGridComponent grid,
                    ref (Box2Rotated WorldBounds,
                        DrawingHandleWorld drawHandle,
                        int gasCount,
                        Texture[][] frames,
                        int[] frameCounter,
                        Texture[][] fireFrames,
                        int[] fireFrameCounter,
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
                        (int) MathF.Floor(floatBounds.Left),
                        (int) MathF.Floor(floatBounds.Bottom),
                        (int) MathF.Ceiling(floatBounds.Right),
                        (int) MathF.Ceiling(floatBounds.Top));

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
                                if (opacity > 0)
                                    state.drawHandle.DrawTexture(state.frames[i][state.frameCounter[i]], tilePosition, Color.White.WithAlpha(opacity));
                            }
                        }
                    }

                    // And again for fire, with the unshaded shader
                    state.drawHandle.UseShader(state.shader);
                    foreach (var chunk in comp.Chunks.Values)
                    {
                        var enumerator = new GasChunkEnumerator(chunk);

                        while (enumerator.MoveNext(out var gas))
                        {
                            if (gas.FireState == 0)
                                continue;

                            var index = chunk.Origin + (enumerator.X, enumerator.Y);
                            if (!localBounds.Contains(index))
                                continue;

                            var fireState = gas.FireState - 1;
                            var texture = state.fireFrames[fireState][state.fireFrameCounter[fireState]];
                            state.drawHandle.DrawTexture(texture, index);
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
}
