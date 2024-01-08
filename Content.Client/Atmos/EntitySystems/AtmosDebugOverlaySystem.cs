using Content.Client.Atmos.Overlays;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Atmos.EntitySystems
{
    [UsedImplicitly]
    internal sealed class AtmosDebugOverlaySystem : SharedAtmosDebugOverlaySystem
    {
        public readonly Dictionary<EntityUid, AtmosDebugOverlayMessage> TileData = new();

        // Configuration set by debug commands and used by AtmosDebugOverlay {
        /// <summary>Value source for display</summary>
        public AtmosDebugOverlayMode CfgMode;
        /// <summary>This is subtracted from value (applied before CfgScale)</summary>
        public float CfgBase = 0;
        /// <summary>The value is divided by this (applied after CfgBase)</summary>
        public float CfgScale = Atmospherics.MolesCellStandard * 2;
        /// <summary>Gas ID used by GasMoles mode</summary>
        public int CfgSpecificGas = 0;
        /// <summary>Uses black-to-white interpolation (as opposed to red-green-blue) for colourblind users</summary>
        public bool CfgCBM = false;
        // }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<AtmosDebugOverlayMessage>(HandleAtmosDebugOverlayMessage);
            SubscribeNetworkEvent<AtmosDebugOverlayDisableMessage>(HandleAtmosDebugOverlayDisableMessage);

            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            if(!overlayManager.HasOverlay<AtmosDebugOverlay>())
                overlayManager.AddOverlay(new AtmosDebugOverlay(this));
        }

        private void OnGridRemoved(GridRemovalEvent ev)
        {
            if (TileData.ContainsKey(ev.EntityUid))
            {
                TileData.Remove(ev.EntityUid);
            }
        }

        private void HandleAtmosDebugOverlayMessage(AtmosDebugOverlayMessage message)
        {
            TileData[GetEntity(message.GridId)] = message;
        }

        private void HandleAtmosDebugOverlayDisableMessage(AtmosDebugOverlayDisableMessage ev)
        {
            TileData.Clear();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            if (overlayManager.HasOverlay<AtmosDebugOverlay>())
                overlayManager.RemoveOverlay<AtmosDebugOverlay>();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            TileData.Clear();
        }

        public bool HasData(EntityUid gridId)
        {
            return TileData.ContainsKey(gridId);
        }
    }

    internal enum AtmosDebugOverlayMode : byte
    {
        TotalMoles,
        GasMoles,
        Temperature
    }
}
