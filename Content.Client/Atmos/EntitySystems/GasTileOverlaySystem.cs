using Content.Client.Atmos.Overlays;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Map;
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
        public readonly float[] Timer = new float[Atmospherics.TotalNumberOfGases];
        public readonly float[][] FrameDelays = new float[Atmospherics.TotalNumberOfGases][];
        public readonly int[] FrameCounter = new int[Atmospherics.TotalNumberOfGases];
        public readonly Texture[][] Frames = new Texture[Atmospherics.TotalNumberOfGases][];

        // Fire overlays
        public const int FireStates = 3;
        public const string FireRsiPath = "/Textures/Effects/fire.rsi";

        public readonly float[] FireTimer = new float[FireStates];
        public readonly float[][] FireFrameDelays = new float[FireStates][];
        public readonly int[] FireFrameCounter = new int[FireStates];
        public readonly Texture[][] FireFrames = new Texture[FireStates][];

        private readonly Dictionary<GridId, Dictionary<Vector2i, GasOverlayChunk>> _tileData =
            new();

        public const int GasOverlayZIndex = 1;

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

                        Frames[i] = state.GetFrames(RSI.State.Direction.South);
                        FrameDelays[i] = state.GetDelays();
                        FrameCounter[i] = 0;
                        break;
                    case SpriteSpecifier.Texture texture:
                        Frames[i] = new[] {texture.Frame0()};
                        FrameDelays[i] = Array.Empty<float>();
                        break;
                    case null:
                        Frames[i] = Array.Empty<Texture>();
                        FrameDelays[i] = Array.Empty<float>();
                        break;
                }
            }

            var fire = _resourceCache.GetResource<RSIResource>(FireRsiPath).RSI;

            for (var i = 0; i < FireStates; i++)
            {
                if (!fire.TryGetState((i+1).ToString(), out var state))
                    throw new ArgumentOutOfRangeException($"Fire RSI doesn't have state \"{i}\"!");

                FireFrames[i] = state.GetFrames(RSI.State.Direction.South);
                FireFrameDelays[i] = state.GetDelays();
                FireFrameCounter[i] = 0;
            }

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            overlayManager.AddOverlay(new GasTileOverlay());
            overlayManager.AddOverlay(new FireTileOverlay());
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
            overlayManager.RemoveOverlay<GasTileOverlay>();
            overlayManager.RemoveOverlay<FireTileOverlay>();
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

            return new GasOverlayEnumerator(overlays, this);
        }

        public FireOverlayEnumerator GetFireOverlays(GridId gridIndex, Vector2i indices)
        {
            if (!_tileData.TryGetValue(gridIndex, out var chunks))
                return default;

            var chunkIndex = GetGasChunkIndices(indices);
            if (!chunks.TryGetValue(chunkIndex, out var chunk))
                return default;

            var overlays = chunk.GetData(indices);

            return new FireOverlayEnumerator(overlays, this);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var delays = FrameDelays[i];
                if (delays.Length == 0) continue;

                var frameCount = FrameCounter[i];
                Timer[i] += frameTime;
                if (!(Timer[i] >= delays[frameCount])) continue;
                Timer[i] = 0f;
                FrameCounter[i] = (frameCount + 1) % Frames[i].Length;
            }

            for (var i = 0; i < FireStates; i++)
            {
                var delays = FireFrameDelays[i];
                if (delays.Length == 0) continue;

                var frameCount = FireFrameCounter[i];
                FireTimer[i] += frameTime;
                if (!(FireTimer[i] >= delays[frameCount])) continue;
                FireTimer[i] = 0f;
                FireFrameCounter[i] = (frameCount + 1) % FireFrames[i].Length;
            }
        }

        public struct GasOverlayEnumerator
        {
            private readonly GasTileOverlaySystem _system;
            private readonly GasData[]? _data;
            // TODO: Take Fire Temperature into account, when we code fire color

            private readonly int _length; // We cache the length so we can avoid a pointer dereference, for speed. Brrr.
            private int _current;

            public GasOverlayEnumerator(in GasOverlayData data, GasTileOverlaySystem system)
            {
                // Gas can't be null, as the caller to this constructor already ensured it wasn't.
                _data = data.Gas;

                _system = system;

                _length = _data?.Length ?? 0;
                _current = 0;
            }

            public bool MoveNext(out (Texture Texture, Color Color) overlay)
            {
                if (_current < _length)
                {
                    // Data can't be null here unless length/current are incorrect
                    var gas = _data![_current++];
                    var frames = _system.Frames[gas.Index];
                    overlay = (frames[_system.FrameCounter[gas.Index]], Color.White.WithAlpha(gas.Opacity));
                    return true;
                }

                overlay = default;
                return false;
            }
        }

        public struct FireOverlayEnumerator
        {
            private readonly GasTileOverlaySystem _system;
            private byte _fireState;
            // TODO: Take Fire Temperature into account, when we code fire color

            public FireOverlayEnumerator(in GasOverlayData data, GasTileOverlaySystem system)
            {
                _fireState = data.FireState;
                _system = system;
            }
            public bool MoveNext(out (Texture Texture, Color Color) overlay)
            {

                if (_fireState != 0)
                {
                    var state = _fireState - 1;
                    var frames = _system.FireFrames[state];
                    // TODO ATMOS Set color depending on temperature
                    overlay = (frames[_system.FireFrameCounter[state]], Color.White);

                    // Setting this to zero so we don't get stuck in an infinite loop.
                    _fireState = 0;
                    return true;
                }

                overlay = default;
                return false;
            }
        }
    }
}
