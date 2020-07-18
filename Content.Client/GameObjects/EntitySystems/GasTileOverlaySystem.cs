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

        private float _timer = 0f;

        public static GasPrototype GetGas(int gasId) =>
            IoCManager.Resolve<IPrototypeManager>().Index<GasPrototype>(gasId.ToString());

        private Dictionary<GridId, Dictionary<MapIndices, GasData[]>> _overlay = new Dictionary<GridId, Dictionary<MapIndices, GasData[]>>();

        public override void Initialize()
        {
            base.Initialize();

            _overlayManager.AddOverlay(new TileOverlay());

            SubscribeNetworkEvent(new EntityEventHandler<GasTileOverlayMessage>(OnTileOverlayMessage));
        }

        public Texture[] GetOverlays(GridId gridIndex, MapIndices indices)
        {
            if (!_overlay.TryGetValue(gridIndex, out var tiles) || !tiles.TryGetValue(indices, out var overlays))
                return new Texture[0];

            var list = new List<Texture>();

            foreach (var gasData in overlays)
            {
                var gas = GetGas(gasData.Index);

                if (gas.GasOverlay is SpriteSpecifier.Rsi animated)
                {
                    var rsi = _resourceCache.GetResource<RSIResource>(animated.RsiPath).RSI;
                    var stateId = animated.RsiState;

                    if(!rsi.TryGetState(stateId, out var state)) continue;

                    list.Add(state.GetFrameAtSecond(RSI.State.Direction.South, _timer));

                    continue;
                }

                var texture = gas.GasOverlay.Frame0();
                list.Add(texture);
            }

            return list.ToArray();
        }

        private void OnTileOverlayMessage(GasTileOverlayMessage ev)
        {
            if(ev.ClearAllOtherOverlays)
                _overlay.Clear();

            foreach (var data in ev.OverlayData)
            {
                if(!_overlay.ContainsKey(data.GridIndex))
                    _overlay[data.GridIndex] = new Dictionary<MapIndices, GasData[]>();

                _overlay[data.GridIndex][data.GridIndices] = data.GasData;
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            _timer += frameTime;
        }
    }
}
