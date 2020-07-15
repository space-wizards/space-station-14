using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class TileOverlaySystem : SharedTileOverlaySystem
    {
        private Dictionary<GridId, Dictionary<MapIndices, List<SpriteSpecifier>>> _overlay = new Dictionary<GridId, Dictionary<MapIndices, List<SpriteSpecifier>>>();

        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private INetManager _netManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public void SetTileOverlay(GridId gridIndex, MapIndices indices, SpriteSpecifier specifier)
        {
            if (!_overlay.ContainsKey(gridIndex))
                _overlay[gridIndex] = new Dictionary<MapIndices, List<SpriteSpecifier>>();

            if (!_overlay[gridIndex].ContainsKey(indices))
                _overlay[gridIndex][indices] = new List<SpriteSpecifier>();

            _overlay[gridIndex][indices].Add(specifier);

            // TODO: Not send this to everyone.
            RaiseNetworkEvent(new TileOverlayMessage(new []
            {
                GetData(gridIndex, indices)
            }));
    }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame) return;

            RaiseNetworkEvent(new TileOverlayMessage(GetData(), true), e.Session.ConnectedClient);
        }

        private TileOverlayData[] GetData()
        {
            var list = new List<TileOverlayData>();

            foreach (var (gridId, tiles) in _overlay)
            {
                foreach (var (indices, _) in tiles)
                {
                    list.Add(GetData(gridId, indices));
                }
            }

            return list.ToArray();
        }

        private TileOverlayData GetData(GridId gridIndex, MapIndices indices)
        {
            var overlay = new List<string>();
            var animated = new List<string>();
            var animatedState = new List<string>();

            if (_overlay.TryGetValue(gridIndex, out var tiles) && tiles.TryGetValue(indices, out var overlays))
            {
                foreach (var specifier in overlays)
                {
                    switch (specifier)
                    {
                        case SpriteSpecifier.Rsi rsi:
                            animated.Add(rsi.RsiPath.ToString());
                            animatedState.Add(rsi.RsiState);
                            break;
                        case SpriteSpecifier.Texture texture:
                            overlay.Add(texture.TexturePath.ToString());
                            break;
                    }
                }
            }

            return new TileOverlayData(gridIndex, indices, overlay.ToArray(),
                animated.ToArray(), animatedState.ToArray());
        }
    }
}
