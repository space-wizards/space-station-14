using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasMixerSystem : EntitySystem
    {
        [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasMixerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasMixerComponent, AtmosDeviceUpdateEvent>(OnMixerUpdated);
            SubscribeLocalEvent<GasMixerComponent, ActivateInWorldEvent>(OnMixerActivate);
            SubscribeLocalEvent<GasMixerComponent, GasAnalyzerScanEvent>(OnMixerAnalyzed);
            // Bound UI subscriptions
            SubscribeLocalEvent<GasMixerComponent, GasMixerChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
            SubscribeLocalEvent<GasMixerComponent, GasMixerChangeNodePercentageMessage>(OnChangeNodePercentageMessage);
            SubscribeLocalEvent<GasMixerComponent, GasMixerToggleStatusMessage>(OnToggleStatusMessage);

            SubscribeLocalEvent<GasMixerComponent, AtmosDeviceDisabledEvent>(OnMixerLeaveAtmosphere);
        }

        private void OnInit(EntityUid uid, GasMixerComponent mixer, ComponentInit args)
        {
            UpdateAppearance(uid, mixer);
        }

        private void OnMixerUpdated(EntityUid uid, GasMixerComponent mixer, ref AtmosDeviceUpdateEvent args)
        {
            // TODO ATMOS: Cache total moles since it's expensive.

            if (!mixer.Enabled)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!_nodeContainer.TryGetNode(nodeContainer, mixer.InletOneName, out PipeNode? inletOne)
                || !_nodeContainer.TryGetNode(nodeContainer, mixer.InletTwoName, out PipeNode? inletTwo)
                || !_nodeContainer.TryGetNode(nodeContainer, mixer.OutletName, out PipeNode? outlet))
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var outputStartingPressure = outlet.Air.Pressure;

            if (outputStartingPressure >= mixer.TargetPressure)
                return; // Target reached, no need to mix.

            var generalTransfer = (mixer.TargetPressure - outputStartingPressure) * outlet.Air.Volume / Atmospherics.R;

            var transferMolesOne = inletOne.Air.Temperature > 0 ? mixer.InletOneConcentration * generalTransfer / inletOne.Air.Temperature : 0f;
            var transferMolesTwo = inletTwo.Air.Temperature > 0 ? mixer.InletTwoConcentration * generalTransfer / inletTwo.Air.Temperature : 0f;

            if (mixer.InletTwoConcentration <= 0f)
            {
                if (inletOne.Air.Temperature <= 0f)
                    return;

                transferMolesOne = MathF.Min(transferMolesOne, inletOne.Air.TotalMoles);
                transferMolesTwo = 0f;
            }

            else if (mixer.InletOneConcentration <= 0)
            {
                if (inletTwo.Air.Temperature <= 0f)
                    return;

                transferMolesOne = 0f;
                transferMolesTwo = MathF.Min(transferMolesTwo, inletTwo.Air.TotalMoles);
            }
            else
            {
                if (inletOne.Air.Temperature <= 0f || inletTwo.Air.Temperature <= 0f)
                    return;

                if (transferMolesOne <= 0 || transferMolesTwo <= 0)
                {
                    _ambientSoundSystem.SetAmbience(uid, false);
                    return;
                }

                if (inletOne.Air.TotalMoles < transferMolesOne || inletTwo.Air.TotalMoles < transferMolesTwo)
                {
                    var ratio = MathF.Min(inletOne.Air.TotalMoles / transferMolesOne, inletTwo.Air.TotalMoles / transferMolesTwo);
                    transferMolesOne *= ratio;
                    transferMolesTwo *= ratio;
                }
            }

            // Actually transfer the gas now.
            var transferred = false;

            if (transferMolesOne > 0f)
            {
                transferred = true;
                var removed = inletOne.Air.Remove(transferMolesOne);
                _atmosphereSystem.Merge(outlet.Air, removed);
            }

            if (transferMolesTwo > 0f)
            {
                transferred = true;
                var removed = inletTwo.Air.Remove(transferMolesTwo);
                _atmosphereSystem.Merge(outlet.Air, removed);
            }

            if (transferred)
                _ambientSoundSystem.SetAmbience(uid, true);
        }

        private void OnMixerLeaveAtmosphere(EntityUid uid, GasMixerComponent mixer, ref AtmosDeviceDisabledEvent args)
        {
            mixer.Enabled = false;

            DirtyUI(uid, mixer);
            UpdateAppearance(uid, mixer);
            _userInterfaceSystem.TryCloseAll(uid, GasFilterUiKey.Key);
        }

        private void OnMixerActivate(EntityUid uid, GasMixerComponent mixer, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            if (Transform(uid).Anchored)
            {
                _userInterfaceSystem.TryOpen(uid, GasMixerUiKey.Key, actor.PlayerSession);
                DirtyUI(uid, mixer);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("comp-gas-mixer-ui-needs-anchor"), args.User);
            }

            args.Handled = true;
        }

        private void DirtyUI(EntityUid uid, GasMixerComponent? mixer)
        {
            if (!Resolve(uid, ref mixer))
                return;

            _userInterfaceSystem.TrySetUiState(uid, GasMixerUiKey.Key,
                new GasMixerBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(uid).EntityName, mixer.TargetPressure, mixer.Enabled, mixer.InletOneConcentration));
        }

        private void UpdateAppearance(EntityUid uid, GasMixerComponent? mixer = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref mixer, ref appearance, false))
                return;

            _appearance.SetData(uid, FilterVisuals.Enabled, mixer.Enabled, appearance);
        }

        private void OnToggleStatusMessage(EntityUid uid, GasMixerComponent mixer, GasMixerToggleStatusMessage args)
        {
            mixer.Enabled = args.Enabled;
            _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
            DirtyUI(uid, mixer);
            UpdateAppearance(uid, mixer);
        }

        private void OnOutputPressureChangeMessage(EntityUid uid, GasMixerComponent mixer, GasMixerChangeOutputPressureMessage args)
        {
            mixer.TargetPressure = Math.Clamp(args.Pressure, 0f, mixer.MaxTargetPressure);
            _adminLogger.Add(LogType.AtmosPressureChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the pressure on {ToPrettyString(uid):device} to {args.Pressure}kPa");
            DirtyUI(uid, mixer);
        }

        private void OnChangeNodePercentageMessage(EntityUid uid, GasMixerComponent mixer,
            GasMixerChangeNodePercentageMessage args)
        {
            float nodeOne = Math.Clamp(args.NodeOne, 0f, 100.0f) / 100.0f;
            mixer.InletOneConcentration = nodeOne;
            mixer.InletTwoConcentration = 1.0f - mixer.InletOneConcentration;
            _adminLogger.Add(LogType.AtmosRatioChanged, LogImpact.Medium,
                $"{EntityManager.ToPrettyString(args.Session.AttachedEntity!.Value):player} set the ratio on {EntityManager.ToPrettyString(uid):device} to {mixer.InletOneConcentration}:{mixer.InletTwoConcentration}");
            DirtyUI(uid, mixer);
        }

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnMixerAnalyzed(EntityUid uid, GasMixerComponent component, GasAnalyzerScanEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            var gasMixDict = new Dictionary<string, GasMixture?>();

            if(_nodeContainer.TryGetNode(nodeContainer, component.InletOneName, out PipeNode? inletOne))
                gasMixDict.Add($"{inletOne.CurrentPipeDirection} {Loc.GetString("gas-analyzer-window-text-inlet")}", inletOne.Air);
            if(_nodeContainer.TryGetNode(nodeContainer, component.InletTwoName, out PipeNode? inletTwo))
                gasMixDict.Add($"{inletTwo.CurrentPipeDirection} {Loc.GetString("gas-analyzer-window-text-inlet")}", inletTwo.Air);
            if(_nodeContainer.TryGetNode(nodeContainer, component.OutletName, out PipeNode? outlet))
                gasMixDict.Add(Loc.GetString("gas-analyzer-window-text-outlet"), outlet.Air);

            args.GasMixtures = gasMixDict;
            args.DeviceFlipped = inletOne != null && inletTwo != null && inletOne.CurrentPipeDirection.ToDirection() == inletTwo.CurrentPipeDirection.ToDirection().GetClockwise90Degrees();
        }
    }
}
