using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Atmos;
using Content.Client.Utility;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private float[] _timer = new float[Atmospherics.TotalNumberOfGases];
        private float[][] _frameDelays = new float[Atmospherics.TotalNumberOfGases][];
        private int[] _frameCounter = new int[Atmospherics.TotalNumberOfGases];
        private readonly Texture[][] _frames = new Texture[Atmospherics.TotalNumberOfGases][];

        private Dictionary<GridId, Dictionary<MapIndices, GasData[]>> _overlay = new Dictionary<GridId, Dictionary<MapIndices, GasData[]>>();

        public override void Initialize()
        {
            base.Initialize();

            _overlayManager.AddOverlay(new GasTileOverlay());

            SubscribeNetworkEvent(new EntityEventHandler<GasTileOverlayMessage>(OnTileOverlayMessage));

            for (int i = 0; i < Atmospherics.TotalNumberOfGases; i++)
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
        }

        public (Texture, float opacity)[] GetOverlays(GridId gridIndex, MapIndices indices)
        {
            if (!_overlay.TryGetValue(gridIndex, out var tiles) || !tiles.TryGetValue(indices, out var overlays))
                return Array.Empty<(Texture, float)>();

            var list = new (Texture, float)[overlays.Length];

            for (var i = 0; i < overlays.Length; i++)
            {
                var gasData = overlays[i];
                var frames = _frames[gasData.Index];
                list[i] = (frames[_frameCounter[gasData.Index]], gasData.Opacity);
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
                	gridOverlays = new Dictionary<MapIndices, GasData[]>();
                    _overlay.Add(data.GridIndex, gridOverlays);
                }

                gridOverlays[data.GridIndices] = data.GasData;
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
                if (_timer[i] >= delays[frameCount])
                {
                    _timer[i] = 0f;
                    _frameCounter[i] = (frameCount + 1) % _frames[i].Length;
                }
            }
        }
    }
}
