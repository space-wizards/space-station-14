#nullable enable
using System;
using System.Collections.Generic;
using Content.Client.Atmos;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private readonly Dictionary<float, Color> _fireCache = new Dictionary<float, Color>();

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
            new Dictionary<GridId, Dictionary<Vector2i, GasOverlayChunk>>();

        private AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<GasOverlayMessage>(HandleGasOverlayMessage);
            _mapManager.OnGridRemoved += OnGridRemoved;

            _atmosphereSystem = Get<AtmosphereSystem>();

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
            if(!overlayManager.HasOverlay(nameof(GasTileOverlay)))
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
            if(!overlayManager.HasOverlay(nameof(GasTileOverlay)))
                overlayManager.RemoveOverlay(nameof(GasTileOverlay));
        }

        private void OnGridRemoved(GridId gridId)
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

        public (Texture, Color color)[] GetOverlays(GridId gridIndex, Vector2i indices)
        {
            if (!_tileData.TryGetValue(gridIndex, out var chunks))
                return Array.Empty<(Texture, Color)>();

            var chunkIndex = GetGasChunkIndices(indices);
            if (!chunks.TryGetValue(chunkIndex, out var chunk))
                return Array.Empty<(Texture, Color)>();

            var overlays = chunk.GetData(indices);

            if (overlays.Gas == null)
                return Array.Empty<(Texture, Color)>();

            var fire = overlays.FireState != 0;
            var length = overlays.Gas.Length + (fire ? 1 : 0);

            var list = new (Texture, Color)[length];

            for (var i = 0; i < overlays.Gas.Length; i++)
            {
                var gasData = overlays.Gas[i];
                var frames = _frames[gasData.Index];
                list[i] = (frames[_frameCounter[gasData.Index]], Color.White.WithAlpha(gasData.Opacity));
            }

            if (fire)
            {
                var state = overlays.FireState - 1;
                var frames = _fireFrames[state];
                // TODO ATMOS Set color depending on temperature
                list[length - 1] = (frames[_fireFrameCounter[state]], Color.White);
            }

            return list;
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
    }
}
