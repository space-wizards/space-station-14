using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Trinary.Components;
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
        [Dependency] private AdminLogSystem _adminLogSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasMixerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasMixerComponent, AtmosDeviceUpdateEvent>(OnMixerUpdated);
            SubscribeLocalEvent<GasMixerComponent, InteractHandEvent>(OnMixerInteractHand);
            // Bound UI subscriptions
            SubscribeLocalEvent<GasMixerComponent, GasMixerChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
            SubscribeLocalEvent<GasMixerComponent, GasMixerChangeNodePercentageMessage>(OnChangeNodePercentageMessage);
            SubscribeLocalEvent<GasMixerComponent, GasMixerToggleStatusMessage>(OnToggleStatusMessage);

            SubscribeLocalEvent<GasMixerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        }

        private void OnAnchorChanged(EntityUid uid, GasMixerComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
                return;

            component.Enabled = false;

            DirtyUI(uid, component);
            UpdateAppearance(uid, component);
            _userInterfaceSystem.TryCloseAll(uid, GasFilterUiKey.Key);
        }

        private void OnInit(EntityUid uid, GasMixerComponent component, ComponentInit args)
        {
            UpdateAppearance(uid, component);
        }

        private void OnMixerUpdated(EntityUid uid, GasMixerComponent mixer, AtmosDeviceUpdateEvent args)
        {
            // TODO ATMOS: Cache total moles since it's expensive.

            if (!mixer.Enabled)
                return;

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(mixer.InletOneName, out PipeNode? inletOne)
                || !nodeContainer.TryGetNode(mixer.InletTwoName, out PipeNode? inletTwo)
                || !nodeContainer.TryGetNode(mixer.OutletName, out PipeNode? outlet))
                return;

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
                    return;

                if (inletOne.Air.TotalMoles < transferMolesOne || inletTwo.Air.TotalMoles < transferMolesTwo)
                {
                    var ratio = MathF.Min(inletOne.Air.TotalMoles / transferMolesOne, inletTwo.Air.TotalMoles / transferMolesTwo);
                    transferMolesOne *= ratio;
                    transferMolesTwo *= ratio;
                }
            }

            // Actually transfer the gas now.

            if (transferMolesOne > 0f)
            {
                var removed = inletOne.Air.Remove(transferMolesOne);
                _atmosphereSystem.Merge(outlet.Air, removed);
            }

            if (transferMolesTwo > 0f)
            {
                var removed = inletTwo.Air.Remove(transferMolesTwo);
                _atmosphereSystem.Merge(outlet.Air, removed);
            }
        }

        private void OnMixerInteractHand(EntityUid uid, GasMixerComponent component, InteractHandEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            if (EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored)
            {
                _userInterfaceSystem.TryOpen(uid, GasMixerUiKey.Key, actor.PlayerSession);
                DirtyUI(uid, component);
            }
            else
            {
                args.User.PopupMessageCursor(Loc.GetString("comp-gas-mixer-ui-needs-anchor"));
            }

            args.Handled = true;
        }

        private void DirtyUI(EntityUid uid, GasMixerComponent? mixer)
        {
            if (!Resolve(uid, ref mixer))
                return;

            _userInterfaceSystem.TrySetUiState(uid, GasMixerUiKey.Key,
                new GasMixerBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(mixer.Owner).EntityName, mixer.TargetPressure, mixer.Enabled, mixer.InletOneConcentration));
        }

        private void UpdateAppearance(EntityUid uid, GasMixerComponent? mixer = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref mixer, ref appearance, false))
                return;

            appearance.SetData(FilterVisuals.Enabled, mixer.Enabled);
        }

        private void OnToggleStatusMessage(EntityUid uid, GasMixerComponent mixer, GasMixerToggleStatusMessage args)
        {
            mixer.Enabled = args.Enabled;
            _adminLogSystem.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
            DirtyUI(uid, mixer);
            UpdateAppearance(uid, mixer);
        }

        private void OnOutputPressureChangeMessage(EntityUid uid, GasMixerComponent mixer, GasMixerChangeOutputPressureMessage args)
        {
            mixer.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);
            _adminLogSystem.Add(LogType.AtmosPressureChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the pressure on {ToPrettyString(uid):device} to {args.Pressure}kPa");
            DirtyUI(uid, mixer);
        }

        private void OnChangeNodePercentageMessage(EntityUid uid, GasMixerComponent mixer,
            GasMixerChangeNodePercentageMessage args)
        {
            float nodeOne = Math.Clamp(args.NodeOne, 0f, 100.0f) / 100.0f;
            mixer.InletOneConcentration = nodeOne;
            mixer.InletTwoConcentration = 1.0f - mixer.InletOneConcentration;
            _adminLogSystem.Add(LogType.AtmosRatioChanged, LogImpact.Medium,
                $"{EntityManager.ToPrettyString(args.Session.AttachedEntity!.Value):player} set the ratio on {EntityManager.ToPrettyString(uid):device} to {mixer.InletOneConcentration}:{mixer.InletTwoConcentration}");
            DirtyUI(uid, mixer);
        }
    }
}
