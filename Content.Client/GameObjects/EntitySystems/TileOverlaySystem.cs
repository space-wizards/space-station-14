using System.Collections.Generic;
using System.Linq;
using Content.Client.Atmos;
using Content.Client.Utility;
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
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class TileOverlaySystem : SharedTileOverlaySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private float _timer = 0f;

        private Dictionary<GridId, Dictionary<MapIndices, HashSet<SpriteSpecifier>>> _overlay = new Dictionary<GridId, Dictionary<MapIndices, HashSet<SpriteSpecifier>>>();

        public override void Initialize()
        {
            base.Initialize();

            _overlayManager.AddOverlay(new TileOverlay());

            SubscribeNetworkEvent(new EntityEventHandler<TileOverlayMessage>(OnTileOverlayMessage));
        }

        public Texture[] GetOverlays(GridId gridIndex, MapIndices indices)
        {
            if (!_overlay.TryGetValue(gridIndex, out var tiles) || !tiles.TryGetValue(indices, out var overlays))
                return new Texture[0];

            var list = new List<Texture>();

            foreach (var overlay in overlays)
            {
                if (overlay is SpriteSpecifier.Rsi animated)
                {
                    var rsi = _resourceCache.GetResource<RSIResource>(animated.RsiPath).RSI;
                    var stateId = animated.RsiState;

                    if(!rsi.TryGetState(stateId, out var state)) continue;

                    list.Add(state.GetFrameAtSecond(RSI.State.Direction.South, _timer));

                    continue;
                }

                var texture = overlay.Frame0();
                list.Add(texture);
            }

            return list.ToArray();
        }

        private void OnTileOverlayMessage(TileOverlayMessage ev)
        {
            if(ev.ClearAllOtherOverlays)
                _overlay.Clear();

            foreach (var data in ev.OverlayData)
            {
                EnsureListExists(data.GridIndex, data.GridIndices);

                foreach (var overlay in data.Overlays)
                {
                    var specifier = new SpriteSpecifier.Texture(new ResourcePath(overlay));
                    _overlay[data.GridIndex][data.GridIndices].Add(specifier);
                }

                for (var i = 0; i < data.AnimatedOverlays.Length; i++)
                {
                    var animated = data.AnimatedOverlays[i];
                    var state = data.AnimatedOverlayStates[i];
                    var specifier = new SpriteSpecifier.Rsi(new ResourcePath(animated), state);
                    _overlay[data.GridIndex][data.GridIndices].Add(specifier);
                }
            }
        }

        private void EnsureListExists(GridId gridIndex, MapIndices indices)
        {
            if (!_overlay.ContainsKey(gridIndex))
                _overlay[gridIndex] = new Dictionary<MapIndices, HashSet<SpriteSpecifier>>();

            if (!_overlay[gridIndex].ContainsKey(indices))
                _overlay[gridIndex][indices] = new HashSet<SpriteSpecifier>();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            _timer += frameTime;
        }
    }
}
