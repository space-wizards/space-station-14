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

        private Dictionary<GridId, Dictionary<Vector2i, GasOverlayChunk>> _tileData =
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

            var list = new (Texture, Color)[overlays.Gas.Length];

            for (var i = 0; i < overlays.Gas.Length; i++)
            {
                var gasData = overlays.Gas[i];
                var frames = _frames[gasData.Index];
                list[i] = (frames[_frameCounter[gasData.Index]], Color.White.WithAlpha(gasData.Opacity));
            }

            return list;
        }

        public (Texture, Color[]) GetFireOverlay(GridId gridIndex, Vector2i indices)
        {

            if (!_tileData.TryGetValue(gridIndex, out var chunks))
                return (Texture.Transparent, new Color[5]);

            var chunkIndex = GetGasChunkIndices(indices);

            if (!chunks.TryGetValue(chunkIndex, out var chunk))
                return (Texture.Transparent, new Color[5]);

            var overlayData = chunk.GetData(indices);

            if (overlayData.FireState == 0)
                return (Texture.Transparent, new Color[5]);       

            var state = overlayData.FireState - 1;
            var frames = _fireFrames[state];

            TryGetFireOverlayColor(gridIndex, indices + new Vector2i(0, 1), out var topColor);
            TryGetFireOverlayColor(gridIndex, indices + new Vector2i(1, 0), out var rightColor);
            TryGetFireOverlayColor(gridIndex, indices + new Vector2i(0, -1), out var bottomColor);
            TryGetFireOverlayColor(gridIndex, indices + new Vector2i(-1, 0), out var leftColor);


            return (frames[_fireFrameCounter[state]], new Color[5] { GetFireColor(overlayData.FireTemperature), topColor, rightColor, bottomColor, leftColor });
        }

        private bool TryGetFireOverlayColor(GridId gridIndex, Vector2i indices, out Color color)
        {
            color = Color.Transparent;

            if (!_tileData.TryGetValue(gridIndex, out var chunks))
                return false;

            var chunkIndex = GetGasChunkIndices(indices);

            if (!chunks.TryGetValue(chunkIndex, out var chunk))
                return false;

            var overlayData = chunk.GetData(indices);

            if (overlayData.FireState == 0)
                return false;

            color = GetFireColor(overlayData.FireTemperature);
            return true;
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


        //Thank you to Goonstation for providing this scientifically correct(?) formula for getting color.
        private Color GetFireColor(float temperature)
        {
            float red, green, blue;

            if (temperature <= 6600)
                red = 255;
            else
                red = 329.698727446f * (float) Math.Pow(temperature/100 - 6000, -0.1332047592f);

            if (temperature <= 6600)
            {
                green = (float)Math.Max(0.001, temperature/100);
                green = 99.4708025861f * (float)Math.Log(green) - 161.1195681661f;
            }
            else
                green = 288.1221695283f * (float) Math.Pow(temperature/100 - 6000, -0.0755148492);

            if (temperature >= 6600)
                blue = 255;
            else if(temperature <= 1900)
                blue = 0;
            else
                blue = 138.5177312231f * (float) Math.Log(temperature/100 - 1000) - 305.0447927307f;


            red = Math.Clamp(red, 0, 255);
            green = Math.Clamp(green, 0, 255);
            blue = Math.Clamp(blue, 0, 255);
            red = red / 255f;
            green = green / 255f;
            blue = blue / 255f;

            return new Color(red, green, blue);

            /*	
            var/input = temperature / 100

            var/red
            if (input <= 66)
                red = 255
            else
                red = input - 60
                red = 329.698727446 * (red ** -0.1332047592)
            red = clamp(red, 0, 255)

            var/green
            if (input <= 66)
                green = max(0.001, input)
                green = 99.4708025861 * log(green) - 161.1195681661
            else
                green = input - 60
                green = 288.1221695283 * (green ** -0.0755148492)
            green = clamp(green, 0, 255)

            var/blue
            if (input >= 66)
                blue = 255
            else
                if (input <= 19)
                    blue = 0
                else
                    blue = input - 10
                    blue = 138.5177312231 * log(blue) - 305.0447927307
            blue = clamp(blue, 0, 255)

            color = rgb(red, green, blue)
            */

        }
    }
}
