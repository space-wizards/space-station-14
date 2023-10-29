using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.Nodes.Components;
using Content.Server.Nodes.Components.Autolinkers;
using Content.Server.Nodes.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

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
        [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
        [Dependency] private readonly AtmosPipeNetSystem _pipeNodeSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasMixerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasMixerComponent, AtmosDeviceUpdateEvent>(OnMixerUpdated);
            SubscribeLocalEvent<GasMixerComponent, InteractHandEvent>(OnMixerInteractHand);
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

        private void OnMixerUpdated(EntityUid uid, GasMixerComponent mixer, AtmosDeviceUpdateEvent args)
        {
            // TODO ATMOS: Cache total moles since it's expensive.

            if (!mixer.Enabled ||
                !TryComp<PolyNodeComponent>(uid, out var poly) ||
                !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>((uid, poly), mixer.OutletName, out var outlet) ||
                !_pipeNodeSystem.TryGetGas((outlet.Owner, outlet.Comp2, outlet.Comp1), out var outletGas))
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var outputStartingPressure = outletGas.Pressure;

            if (outputStartingPressure >= mixer.TargetPressure || // Target reached, no need to mix.
                !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, mixer.InletOneName, out var inletOne) ||
                !_pipeNodeSystem.TryGetGas((inletOne.Owner, inletOne.Comp2, inletOne.Comp1), out var inletOneGas) ||
                !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, mixer.InletTwoName, out var inletTwo) ||
                !_pipeNodeSystem.TryGetGas((inletTwo.Owner, inletTwo.Comp2, inletTwo.Comp1), out var inletTwoGas))
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var generalTransfer = (mixer.TargetPressure - outputStartingPressure) * outletGas.Volume / Atmospherics.R;

            var transferMolesOne = inletOneGas.Temperature > 0 ? mixer.InletOneConcentration * generalTransfer / inletOneGas.Temperature : 0f;
            var transferMolesTwo = inletTwoGas.Temperature > 0 ? mixer.InletTwoConcentration * generalTransfer / inletTwoGas.Temperature : 0f;

            if (mixer.InletTwoConcentration <= 0f)
            {
                if (inletOneGas.Temperature <= 0f)
                    return;

                transferMolesOne = MathF.Min(transferMolesOne, inletOneGas.TotalMoles);
                transferMolesTwo = 0f;
            }

            else if (mixer.InletOneConcentration <= 0)
            {
                if (inletTwoGas.Temperature <= 0f)
                    return;

                transferMolesOne = 0f;
                transferMolesTwo = MathF.Min(transferMolesTwo, inletTwoGas.TotalMoles);
            }
            else
            {
                if (inletOneGas.Temperature <= 0f || inletTwoGas.Temperature <= 0f)
                    return;

                if (transferMolesOne <= 0 || transferMolesTwo <= 0)
                {
                    _ambientSoundSystem.SetAmbience(uid, false);
                    return;
                }

                if (inletOneGas.TotalMoles < transferMolesOne || inletTwoGas.TotalMoles < transferMolesTwo)
                {
                    var ratio = MathF.Min(inletOneGas.TotalMoles / transferMolesOne, inletTwoGas.TotalMoles / transferMolesTwo);
                    transferMolesOne *= ratio;
                    transferMolesTwo *= ratio;
                }
            }

            // Actually transfer the gas now.
            var transferred = false;

            if (transferMolesOne > 0f)
            {
                transferred = true;
                var removed = inletOneGas.Remove(transferMolesOne);
                _atmosphereSystem.Merge(outletGas, removed);
            }

            if (transferMolesTwo > 0f)
            {
                transferred = true;
                var removed = inletTwoGas.Remove(transferMolesTwo);
                _atmosphereSystem.Merge(outletGas, removed);
            }

            if (transferred)
                _ambientSoundSystem.SetAmbience(uid, true);
        }

        private void OnMixerLeaveAtmosphere(EntityUid uid, GasMixerComponent mixer, AtmosDeviceDisabledEvent args)
        {
            mixer.Enabled = false;

            DirtyUI(uid, mixer);
            UpdateAppearance(uid, mixer);
            _userInterfaceSystem.TryCloseAll(uid, GasFilterUiKey.Key);
        }

        private void OnMixerInteractHand(EntityUid uid, GasMixerComponent mixer, InteractHandEvent args)
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
                _popupSystem.PopupCursor(Loc.GetString("comp-gas-mixer-ui-needs-anchor"), args.User);
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
            if (!TryComp<PolyNodeComponent>(uid, out var poly))
                return;

            var gasMixDict = new Dictionary<string, GasMixture?>();

            if (_nodeSystem.TryGetNode<AtmosPipeNodeComponent, DirNodeComponent>((uid, poly), component.InletOneName, out var inletOne)
            && _pipeNodeSystem.TryGetGas((inletOne.Owner, inletOne.Comp2, inletOne.Comp1), out var inletOneGas))
                gasMixDict.Add($"{inletOne.Comp3.CurrentDirection} {Loc.GetString("gas-analyzer-window-text-inlet")}", inletOneGas);

            if (_nodeSystem.TryGetNode<AtmosPipeNodeComponent, DirNodeComponent>((uid, poly), component.InletTwoName, out var inletTwo)
            && _pipeNodeSystem.TryGetGas((inletTwo.Owner, inletTwo.Comp2, inletTwo.Comp1), out var inletTwoGas))
                gasMixDict.Add($"{inletTwo.Comp3.CurrentDirection} {Loc.GetString("gas-analyzer-window-text-inlet")}", inletTwoGas);

            if (_nodeSystem.TryGetNode<AtmosPipeNodeComponent>((uid, poly), component.OutletName, out var outlet)
            && _pipeNodeSystem.TryGetGas((outlet.Owner, outlet.Comp2, outlet.Comp1), out var outletGas))
                gasMixDict.Add(Loc.GetString("gas-analyzer-window-text-outlet"), outletGas);

            args.GasMixtures = gasMixDict;
            args.DeviceFlipped = inletOne.Comp3 is { } inletOneDir && inletTwo.Comp3 is { } inletTwoDir && inletOneDir.CurrentDirection.ToDirection() == inletTwoDir.CurrentDirection.ToDirection().GetClockwise90Degrees();
        }
    }
}
