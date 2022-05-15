using Content.Client.Atmos.Overlays;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Atmos.EntitySystems
{
    [UsedImplicitly]
    internal sealed class AtmosDebugOverlaySystem : SharedAtmosDebugOverlaySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        private readonly Dictionary<GridId, AtmosDebugOverlayMessage> _tileData =
            new();

        // Configuration set by debug commands and used by AtmosDebugBlockOverlay and AtmosDebugGraphOverlay {
        /// <summary>Types of info to draw on the overlay</summary>
        public AtmosDebugShowMode CfgMode = DisplayModes[(int) AtmosDebugStyle.Block];

        /// <summary>Display style (block or graph)</summary>
        public AtmosDebugStyle CfgStyle
        {
            get => _style;
            set
            {
                if (value == _style) return;

                DisplayModes[(int)_style] = CfgMode;
                _style = value;
                CfgMode = DisplayModes[(int)value];
                ResetScale();
                EnsureCorrectOverlayIsShown();
            }
        }

        /// <summary>This is subtracted from value (applied before CfgScale)</summary>
        public float CfgBase = 0;
        /// <summary>The value is divided by this (applied after CfgBase)</summary>
        public float CfgScale = Atmospherics.MolesCellStandard * 2;
        /// <summary>Gas ID used by GasMoles mode, block style</summary>
        public int CfgSpecificGas = 0;
        /// <summary>Uses black-to-white interpolation (as opposed to red-green-blue) for colourblind users in block style</summary>
        public bool CfgCBM = false;
        // }

        private AtmosDebugStyle _style = AtmosDebugStyle.Block;

        // defaults which are then used to cache when switching styles
        private static readonly AtmosDebugShowMode[] DisplayModes = {
            // for Block
            AtmosDebugShowMode.TotalMoles
                | AtmosDebugShowMode.BlockDirections | AtmosDebugShowMode.ExcitedGroups
                | AtmosDebugShowMode.FlowDirections,
            // for Graph
            AtmosDebugShowMode.TotalMoles | AtmosDebugShowMode.GasMoles | AtmosDebugShowMode.Temperature
                | AtmosDebugShowMode.BlockDirections | AtmosDebugShowMode.ExcitedGroups
                | AtmosDebugShowMode.FlowDirections
        };

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<AtmosDebugOverlayMessage>(HandleAtmosDebugOverlayMessage);
            SubscribeNetworkEvent<AtmosDebugOverlayDisableMessage>(HandleAtmosDebugOverlayDisableMessage);

            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

            // default to block style
            CfgStyle = AtmosDebugStyle.Block;
        }

        private void EnsureCorrectOverlayIsShown()
        {
            switch (_style)
            {
                case AtmosDebugStyle.Block:
                    _overlayManager.RemoveOverlay<AtmosDebugGraphOverlay>();
                    _overlayManager.AddOverlay(new AtmosDebugBlockOverlay());
                    break;
                case AtmosDebugStyle.Graph:
                    _overlayManager.RemoveOverlay<AtmosDebugBlockOverlay>();
                    _overlayManager.AddOverlay(new AtmosDebugGraphOverlay());
                    break;
                default:
                    throw new InvalidOperationException("CfgStyle set to unknown display style for atmos debug overlay");
            }
        }

        private void OnGridRemoved(GridRemovalEvent ev)
        {
            if (_tileData.ContainsKey(ev.GridId))
            {
                _tileData.Remove(ev.GridId);
            }
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
            _overlayManager.RemoveOverlay<AtmosDebugBlockOverlay>();
            _overlayManager.RemoveOverlay<AtmosDebugGraphOverlay>();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _tileData.Clear();
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

            return srcMsg.OverlayData[relative.X + relative.Y * LocalViewRange];
        }

        public void SwitchMode(AtmosDebugShowMode modeToSwitch)
        {
            // swap between these modes in block style
            if (_style == AtmosDebugStyle.Block && modeToSwitch < AtmosDebugShowMode.BlockDirections)
            {
                CfgMode &= ~(AtmosDebugShowMode.TotalMoles | AtmosDebugShowMode.GasMoles |
                             AtmosDebugShowMode.Temperature);
                CfgMode |= modeToSwitch;
                ResetScale();
                return;
            }

            if ((CfgMode & modeToSwitch) == 0)
                CfgMode |= modeToSwitch;
            else
                CfgMode &= ~modeToSwitch;
        }

        private void ResetScale()
        {
            if (_style == AtmosDebugStyle.Block && (CfgMode & AtmosDebugShowMode.Temperature) != 0)
            {
                // Red is 100C, Green is 20C, Blue is -60C
                CfgBase = Atmospherics.T20C + 80;
                CfgScale = -160;
            }
            else
            {
                CfgBase = 0;
                CfgScale = Atmospherics.MolesCellStandard * 2;
            }
        }
    }

    [Flags]
    internal enum AtmosDebugShowMode : byte
    {
        TotalMoles = 1 << 0,
        GasMoles = 1 << 1,
        Temperature = 1 << 2,
        BlockDirections = 1 << 3,
        FlowDirections = 1 << 4,
        ExcitedGroups = 1 << 5
    }

    internal enum AtmosDebugStyle : byte
    {
        Block = 0,
        Graph = 1
    }
}
