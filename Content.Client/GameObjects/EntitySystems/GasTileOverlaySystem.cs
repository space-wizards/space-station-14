using System;
using System.Collections.Generic;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
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

        private Dictionary<GridId, Dictionary<MapIndices, GasOverlayData>> _overlay = new Dictionary<GridId, Dictionary<MapIndices, GasOverlayData>>();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent(new EntityEventHandler<GasTileOverlayMessage>(OnTileOverlayMessage));

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gas = Atmospherics.GetGas(i);
                switch (gas.GasOverlay)
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
        }

        public (Texture, Color color)[] GetOverlays(GridId gridIndex, MapIndices indices)
        {
            if (!_overlay.TryGetValue(gridIndex, out var tiles) || !tiles.TryGetValue(indices, out var overlays))
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

        private void OnTileOverlayMessage(GasTileOverlayMessage ev)
        {
            if(ev.ClearAllOtherOverlays)
                _overlay.Clear();

            foreach (var data in ev.OverlayData)
            {
                if (!_overlay.TryGetValue(data.GridIndex, out var gridOverlays))
                {
                	gridOverlays = new Dictionary<MapIndices, GasOverlayData>();
                    _overlay.Add(data.GridIndex, gridOverlays);
                }

                gridOverlays[data.GridIndices] = data.Data;
            }
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
