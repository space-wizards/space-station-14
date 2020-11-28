#nullable enable
using System.Collections.Generic;
using Content.Client.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class AtmosDebugOverlaySystem : SharedAtmosDebugOverlaySystem, IResettingEntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Dictionary<GridId, AtmosDebugOverlayMessage> _tileData =
            new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<AtmosDebugOverlayMessage>(HandleAtmosDebugOverlayMessage);
            SubscribeNetworkEvent<AtmosDebugOverlayDisableMessage>(HandleAtmosDebugOverlayDisableMessage);

            _mapManager.OnGridRemoved += OnGridRemoved;

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            if(!overlayManager.HasOverlay(nameof(AtmosDebugOverlay)))
                overlayManager.AddOverlay(new AtmosDebugOverlay());
        }

        private void HandleAtmosDebugOverlayMessage(AtmosDebugOverlayMessage message)
        {
            _tileData[message.GridId] = message;
        }

        private void HandleAtmosDebugOverlayDisableMessage(AtmosDebugOverlayDisableMessage ev)
        {
            _tileData.Clear();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.OnGridRemoved -= OnGridRemoved;
            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            if(!overlayManager.HasOverlay(nameof(GasTileOverlay)))
                overlayManager.RemoveOverlay(nameof(GasTileOverlay));
        }

        public void Reset()
        {
            _tileData.Clear();
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

        public AtmosDebugOverlayData? GetData(GridId gridIndex, Vector2i indices)
        {
            if (!_tileData.TryGetValue(gridIndex, out var srcMsg))
                return null;

            var relative = indices - srcMsg.BaseIdx;
            if (relative.X < 0 || relative.Y < 0 || relative.X >= LocalViewRange || relative.Y >= LocalViewRange)
                return null;

            return srcMsg.OverlayData[relative.X + (relative.Y * LocalViewRange)];
        }
    }
}
