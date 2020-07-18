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
    public sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        private int _tickTimer = 0;
        private List<GasTileOverlayData> _queue = new List<GasTileOverlayData>();

        private Dictionary<GridId, Dictionary<MapIndices, GasData[]>> _overlay =
            new Dictionary<GridId, Dictionary<MapIndices, GasData[]>>();

        [Dependency] private IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public void SetTileOverlay(GridId gridIndex, MapIndices indices, GasData[] gasData)
        {
            if(!_overlay.TryGetValue(gridIndex, out var _))
                _overlay[gridIndex] = new Dictionary<MapIndices, GasData[]>();

            _overlay[gridIndex][indices] = gasData;
            _queue.Add(GetData(gridIndex, indices));
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame) return;

            RaiseNetworkEvent(new GasTileOverlayMessage(GetData(), true), e.Session.ConnectedClient);
        }

        private GasTileOverlayData[] GetData()
        {
            var list = new List<GasTileOverlayData>();

            foreach (var (gridId, tiles) in _overlay)
            {
                foreach (var (indices, _) in tiles)
                {
                    var data = GetData(gridId, indices);
                    if(data.GasData.Length > 0)
                        list.Add(data);
                }
            }

            return list.ToArray();
        }

        private GasTileOverlayData GetData(GridId gridIndex, MapIndices indices)
        {
            return new GasTileOverlayData(gridIndex, indices, _overlay[gridIndex][indices] ?? new GasData[0]);
        }

        public override void Update(float frameTime)
        {
            _tickTimer++;

            if (_tickTimer < 10) return;

            _tickTimer = 0;
            RaiseNetworkEvent(new GasTileOverlayMessage(_queue.ToArray()));
            _queue.Clear();
        }
    }
}
