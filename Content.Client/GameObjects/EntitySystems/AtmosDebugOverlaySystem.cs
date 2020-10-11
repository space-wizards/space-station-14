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
    internal sealed class AtmosDebugOverlaySystem : SharedAtmosDebugOverlaySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        private Dictionary<GridId, AtmosDebugOverlayMessage> _tileData =
            new Dictionary<GridId, AtmosDebugOverlayMessage>();

        private AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<AtmosDebugOverlayMessage>(HandleAtmosDebugOverlayMessage);
            _mapManager.OnGridRemoved += OnGridRemoved;

            _atmosphereSystem = Get<AtmosphereSystem>();

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            if(!overlayManager.HasOverlay(nameof(AtmosDebugOverlay)))
                overlayManager.AddOverlay(new AtmosDebugOverlay());
        }

        private void HandleAtmosDebugOverlayMessage(AtmosDebugOverlayMessage message)
        {
            _tileData[message.GridId] = message;
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
