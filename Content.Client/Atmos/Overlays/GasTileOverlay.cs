using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.Overlays
{
    public sealed class GasTileOverlay : Overlay
    {
        private readonly IEntityManager _entManager;
        private readonly IMapManager _mapManager;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
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

        public GasTileOverlay(GasTileOverlaySystem system, IEntityManager entManager, IResourceCache resourceCache, IPrototypeManager protoMan, SpriteSystem spriteSys)
        {
            _entManager = entManager;
            _mapManager = IoCManager.Resolve<IMapManager>();
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
                    overlay = new SpriteSpecifier.Rsi(new ResourcePath(gasPrototype.GasOverlaySprite), gasPrototype.GasOverlayState);
                else if (!string.IsNullOrEmpty(gasPrototype.GasOverlayTexture))
                    overlay = new SpriteSpecifier.Texture(new ResourcePath(gasPrototype.GasOverlayTexture));
                else
                    continue;

                switch (overlay)
                {
                    case SpriteSpecifier.Rsi animated:
                        var rsi = resourceCache.GetResource<RSIResource>(animated.RsiPath).RSI;
                        var stateId = animated.RsiState;

                        if (!rsi.TryGetState(stateId, out var state)) continue;

                        _frames[i] = state.GetFrames(RSI.State.Direction.South);
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

                _fireFrames[i] = state.GetFrames(RSI.State.Direction.South);
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
                if (delays.Length == 0) continue;

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
                if (delays.Length == 0) continue;

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
            var drawHandle = args.WorldHandle;
            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
            var overlayQuery = _entManager.GetEntityQuery<GasTileOverlayComponent>();

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(args.MapId, args.WorldBounds))
            {
                if (!overlayQuery.TryGetComponent(mapGrid.Owner, out var comp) ||
                    !xformQuery.TryGetComponent(mapGrid.Owner, out var gridXform))
                {
                    continue;
                }

                var (_, _, worldMatrix, invMatrix) = gridXform.GetWorldPositionRotationMatrixWithInv();
                drawHandle.SetTransform(worldMatrix);
                var floatBounds = invMatrix.TransformBox(in args.WorldBounds).Enlarged(mapGrid.TileSize);
                var localBounds = new Box2i(
                    (int) MathF.Floor(floatBounds.Left),
                    (int) MathF.Floor(floatBounds.Bottom),
                    (int) MathF.Ceiling(floatBounds.Right),
                    (int) MathF.Ceiling(floatBounds.Top));

                // Currently it would be faster to group drawing by gas rather than by chunk, but if the textures are
                // ever moved to a single atlas, that should no longer be the case. So this is just grouping draw calls
                // by chunk, even though its currently slower.

                drawHandle.UseShader(null);
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

                        for (var i = 0; i < _gasCount; i++)
                        {
                            var opacity = gas.Opacity[i];
                            if (opacity > 0)
                                drawHandle.DrawTexture(_frames[i][_frameCounter[i]], tilePosition, Color.White.WithAlpha(opacity));
                        }
                    }
                }

                // And again for fire, with the unshaded shader
                drawHandle.UseShader(_shader);
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

                        var state = gas.FireState - 1;
                        var texture = _fireFrames[state][_fireFrameCounter[state]];
                        drawHandle.DrawTexture(texture, index);
                    }
                }
            }

            drawHandle.UseShader(null);
            drawHandle.SetTransform(Matrix3.Identity);
        }
    }
}
