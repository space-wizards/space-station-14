using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Client.Atmos.Overlays;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        // Gas overlays
        private readonly float[] _timer = new float[Atmospherics.TotalNumberOfGases];
        private readonly float[][] _frameDelays = new float[Atmospherics.TotalNumberOfGases][];
        private readonly int[] _frameCounter = new int[Atmospherics.TotalNumberOfGases];
        private readonly Texture[][] _frames = new Texture[Atmospherics.TotalNumberOfGases][];

        // Fire overlays
        private const int FireStates = 3;
        private const string FireRsiPath = "/Textures/Effects/fire.rsi";

        private readonly float[] _fireTimer = new float[FireStates];
        private readonly float[][] _fireFrameDelays = new float[FireStates][];
        private readonly int[] _fireFrameCounter = new int[FireStates];
        private readonly Texture[][] _fireFrames = new Texture[FireStates][];

        private readonly Dictionary<GridId, Dictionary<Vector2i, GasOverlayChunk>> _tileData =
            new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<GasOverlayMessage>(HandleGasOverlayMessage);
            _mapManager.OnGridRemoved += OnGridRemoved;

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var overlay = _atmosphereSystem.GetOverlay(i);
                switch (overlay)
                {
                    case SpriteSpecifier.Rsi animated:
                        var rsi = _resourceCache.GetResource<RSIResource>(animated.RsiPath).RSI;
                        var stateId = animated.RsiState;

                        if(!rsi.TryGetState(stateId, out var state)) continue;

                        _frames[i] = state.GetFrames(RSI.State.Direction.South);
                        _frameDelays[i] = state.GetDelays();
                        _frameCounter[i] = 0;
                        break;
                    case SpriteSpecifier.Texture texture:
                        _frames[i] = new[] {texture.Frame0()};
                        _frameDelays[i] = Array.Empty<float>();
                        break;
                    case null:
                        _frames[i] = Array.Empty<Texture>();
                        _frameDelays[i] = Array.Empty<float>();
                        break;
                }
            }

            var fire = _resourceCache.GetResource<RSIResource>(FireRsiPath).RSI;

            for (var i = 0; i < FireStates; i++)
            {
                if (!fire.TryGetState((i+1).ToString(), out var state))
                    throw new ArgumentOutOfRangeException($"Fire RSI doesn't have state \"{i}\"!");

                _fireFrames[i] = state.GetFrames(RSI.State.Direction.South);
                _fireFrameDelays[i] = state.GetDelays();
                _fireFrameCounter[i] = 0;
            }

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            if(!overlayManager.HasOverlay<GasTileOverlay>())
                overlayManager.AddOverlay(new GasTileOverlay());
        }

        private void HandleGasOverlayMessage(GasOverlayMessage message)
        {
            foreach (var (indices, data) in message.OverlayData)
            {
                var chunk = GetOrCreateChunk(message.GridId, indices);
                chunk.Update(data, indices);
            }
        }

        // Slightly different to the server-side system version
        private GasOverlayChunk GetOrCreateChunk(GridId gridId, Vector2i indices)
        {
            if (!_tileData.TryGetValue(gridId, out var chunks))
            {
                chunks = new Dictionary<Vector2i, GasOverlayChunk>();
                _tileData[gridId] = chunks;
            }

            var chunkIndices = GetGasChunkIndices(indices);

            if (!chunks.TryGetValue(chunkIndices, out var chunk))
            {
                chunk = new GasOverlayChunk(gridId, chunkIndices);
                chunks[chunkIndices] = chunk;
            }

            return chunk;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.OnGridRemoved -= OnGridRemoved;
            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            if(!overlayManager.HasOverlay<GasTileOverlay>())
                overlayManager.RemoveOverlay<GasTileOverlay>();
        }

        private void OnGridRemoved(MapId mapId, GridId gridId)
        {
            if (_tileData.ContainsKey(gridId))
            {
                _tileData.Remove(gridId);
            }
        }

        public bool HasData(GridId gridId)
        {
            return _tileData.ContainsKey(gridId);
        }

        public GasOverlayEnumerator GetOverlays(GridId gridIndex, Vector2i indices)
        {
            if (!_tileData.TryGetValue(gridIndex, out var chunks))
                return default;

            var chunkIndex = GetGasChunkIndices(indices);
            if (!chunks.TryGetValue(chunkIndex, out var chunk))
                return default;

            var overlays = chunk.GetData(indices);

            return new GasOverlayEnumerator(overlays,
                in _frames, in _fireFrames, in _frameCounter, in _fireFrameCounter);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var delays = _frameDelays[i];
                if (delays.Length == 0) continue;

                var frameCount = _frameCounter[i];
                _timer[i] += frameTime;
                if (!(_timer[i] >= delays[frameCount])) continue;
                _timer[i] = 0f;
                _frameCounter[i] = (frameCount + 1) % _frames[i].Length;
            }

            for (var i = 0; i < FireStates; i++)
            {
                var delays = _fireFrameDelays[i];
                if (delays.Length == 0) continue;

                var frameCount = _fireFrameCounter[i];
                _fireTimer[i] += frameTime;
                if (!(_fireTimer[i] >= delays[frameCount])) continue;
                _fireTimer[i] = 0f;
                _fireFrameCounter[i] = (frameCount + 1) % _fireFrames[i].Length;
            }
        }

        public struct GasOverlayEnumerator : IDisposable
        {
            private readonly Texture[][] _frames;
            private readonly Texture[][] _fireFrames;

            private readonly int[] _frameCounter;
            private readonly int[] _fireFrameCounter;

            private readonly GasData[]? _data;
            private byte _fireState;
            // TODO: Take Fire Temperature into account, when we code fire color

            private readonly int _length; // We cache the length so we can avoid a pointer dereference, for speed. Brrr.
            private int _current;

            public GasOverlayEnumerator(in GasOverlayData data, in Texture[][] frames, in Texture[][] fireFrames, in int[] frameCounter, in int[] fireFrameCounter)
            {
                // Gas can't be null, as the caller to this constructor already ensured it wasn't.
                _data = data.Gas;
                _fireState = data.FireState;

                _frames = frames;
                _fireFrames = fireFrames;

                _frameCounter = frameCounter;
                _fireFrameCounter = fireFrameCounter;

                _length = _data?.Length ?? 0;
                _current = 0;
            }

            public bool MoveNext(out (Texture Texture, Color Color) overlay)
            {
                if (_current < _length)
                {
                    // Data can't be null here unless length/current are incorrect
                    var gas = _data![_current++];
                    var frames = _frames[gas.Index];
                    overlay = (frames[_frameCounter[gas.Index]], Color.White.WithAlpha(gas.Opacity));
                    return true;
                }

                if (_fireState != 0)
                {
                    var state = _fireState - 1;
                    var frames = _fireFrames[state];
                    // TODO ATMOS Set color depending on temperature
                    overlay = (frames[_fireFrameCounter[state]], Color.White);

                    // Setting this to zero so we don't get stuck in an infinite loop.
                    _fireState = 0;
                    return true;
                }

                overlay = default;
                return false;
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
}
