using Content.Client.Atmos.Overlays;
using Content.Client.Overlays;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosDebugOverlaySystem : DebugOverlaySystem<AtmosDebugOverlay, AtmosDebugOverlayMessage>
    {
        
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
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
        }

        protected override void OnRecievedPayload(AtmosDebugOverlayMessage message)
        {
            // no-op
        }

        private void OnGridRemoved(GridRemovalEvent ev)
        {
            if (_currentOverlay != null && _currentOverlay.TileData.ContainsKey(ev.EntityUid))
            {
                _currentOverlay.TileData.Remove(ev.EntityUid);
            }
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            if (_currentOverlay != null)
            {
                _currentOverlay.TileData.Clear();
            }
        }

        public bool HasData(EntityUid gridId)
        {
            if (_currentOverlay != null)
            {
                return _currentOverlay.TileData.ContainsKey(gridId);
            }
            else
            {
                return false;
            }
        }
    }

    public enum AtmosDebugOverlayMode : byte
    {
        TotalMoles,
        GasMoles,
        Temperature
    }
}
