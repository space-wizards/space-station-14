using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Atmos.Piping.Trinary.Components;
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
    public sealed class GasFilterSystem : EntitySystem
    {
        [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
        [Dependency] private readonly AtmosPipeNetSystem _pipeNodeSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasFilterComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasFilterComponent, AtmosDeviceUpdateEvent>(OnFilterUpdated);
            SubscribeLocalEvent<GasFilterComponent, AtmosDeviceDisabledEvent>(OnFilterLeaveAtmosphere);
            SubscribeLocalEvent<GasFilterComponent, InteractHandEvent>(OnFilterInteractHand);
            SubscribeLocalEvent<GasFilterComponent, GasAnalyzerScanEvent>(OnFilterAnalyzed);
            // Bound UI subscriptions
            SubscribeLocalEvent<GasFilterComponent, GasFilterChangeRateMessage>(OnTransferRateChangeMessage);
            SubscribeLocalEvent<GasFilterComponent, GasFilterSelectGasMessage>(OnSelectGasMessage);
            SubscribeLocalEvent<GasFilterComponent, GasFilterToggleStatusMessage>(OnToggleStatusMessage);

        }

        private void OnInit(EntityUid uid, GasFilterComponent filter, ComponentInit args)
        {
            UpdateAppearance(uid, filter);
        }

        private void OnFilterUpdated(EntityUid uid, GasFilterComponent filterDevice, AtmosDeviceUpdateEvent args)
        {
            if (!filterDevice.Enabled
            || !TryComp<AtmosDeviceComponent>(uid, out var device)
            || !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, filterDevice.InletName, out var inletId, out var inletNode, out var inlet)
            || !_pipeNodeSystem.TryGetGas(inletId, out var inletGas, inlet, inletNode)
            || !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, filterDevice.FilterName, out var filterId, out var filterNode, out var filter)
            || !_pipeNodeSystem.TryGetGas(filterId, out var filterGas, filter, filterNode)
            || !_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, filterDevice.OutletName, out var outletId, out var outletNode, out var outlet)
            || !_pipeNodeSystem.TryGetGas(outletId, out var outletGas, outlet, outletNode)
            || outletGas.Pressure >= Atmospherics.MaxOutputPressure) // No need to transfer if target is full.
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var transferVol = filterDevice.TransferRate * args.dt;

            if (transferVol <= 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var removed = inletGas.RemoveVolume(transferVol);

            if (filterDevice.FilteredGas.HasValue)
            {
                var filteredOut = new GasMixture() { Temperature = removed.Temperature };

                filteredOut.SetMoles(filterDevice.FilteredGas.Value, removed.GetMoles(filterDevice.FilteredGas.Value));
                removed.SetMoles(filterDevice.FilteredGas.Value, 0f);

                var target = filterGas.Pressure < Atmospherics.MaxOutputPressure ? filterNode : inletNode;
                _atmosphereSystem.Merge(filterGas, filteredOut);
                _ambientSoundSystem.SetAmbience(uid, filteredOut.TotalMoles > 0f);
            }

            _atmosphereSystem.Merge(outletGas, removed);
        }

        private void OnFilterLeaveAtmosphere(EntityUid uid, GasFilterComponent filter, AtmosDeviceDisabledEvent args)
        {
            filter.Enabled = false;

            UpdateAppearance(uid, filter);
            _ambientSoundSystem.SetAmbience(uid, false);

            DirtyUI(uid, filter);
            _userInterfaceSystem.TryCloseAll(uid, GasFilterUiKey.Key);
        }

        private void OnFilterInteractHand(EntityUid uid, GasFilterComponent filter, InteractHandEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            if (EntityManager.GetComponent<TransformComponent>(uid).Anchored)
            {
                _userInterfaceSystem.TryOpen(uid, GasFilterUiKey.Key, actor.PlayerSession);
                DirtyUI(uid, filter);
            }
            else
            {
                _popupSystem.PopupCursor(Loc.GetString("comp-gas-filter-ui-needs-anchor"), args.User);
            }

            args.Handled = true;
        }

        private void DirtyUI(EntityUid uid, GasFilterComponent? filter)
        {
            if (!Resolve(uid, ref filter))
                return;

            _userInterfaceSystem.TrySetUiState(uid, GasFilterUiKey.Key,
                new GasFilterBoundUserInterfaceState(MetaData(uid).EntityName, filter.TransferRate, filter.Enabled, filter.FilteredGas));
        }

        private void UpdateAppearance(EntityUid uid, GasFilterComponent? filter = null)
        {
            if (!Resolve(uid, ref filter, false))
                return;

            _appearanceSystem.SetData(uid, FilterVisuals.Enabled, filter.Enabled);
        }

        private void OnToggleStatusMessage(EntityUid uid, GasFilterComponent filter, GasFilterToggleStatusMessage args)
        {
            filter.Enabled = args.Enabled;
            _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
            DirtyUI(uid, filter);
            UpdateAppearance(uid, filter);
        }

        private void OnTransferRateChangeMessage(EntityUid uid, GasFilterComponent filter, GasFilterChangeRateMessage args)
        {
            filter.TransferRate = Math.Clamp(args.Rate, 0f, filter.MaxTransferRate);
            _adminLogger.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the transfer rate on {ToPrettyString(uid):device} to {args.Rate}");
            DirtyUI(uid, filter);

        }

        private void OnSelectGasMessage(EntityUid uid, GasFilterComponent filter, GasFilterSelectGasMessage args)
        {
            if (args.ID.HasValue)
            {
                if (Enum.TryParse<Gas>(args.ID.ToString(), true, out var parsedGas))
                {
                    filter.FilteredGas = parsedGas;
                    _adminLogger.Add(LogType.AtmosFilterChanged, LogImpact.Medium,
                        $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the filter on {ToPrettyString(uid):device} to {parsedGas.ToString()}");
                    DirtyUI(uid, filter);
                }
                else
                {
                    Logger.Warning("atmos", $"{ToPrettyString(uid)} received GasFilterSelectGasMessage with an invalid ID: {args.ID}");
                }
            }
            else
            {
                filter.FilteredGas = null;
                _adminLogger.Add(LogType.AtmosFilterChanged, LogImpact.Medium,
                    $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the filter on {ToPrettyString(uid):device} to none");
                DirtyUI(uid, filter);
            }
        }

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnFilterAnalyzed(EntityUid uid, GasFilterComponent component, GasAnalyzerScanEvent args)
        {
            var gasMixDict = new Dictionary<string, GasMixture?>();

            if (_nodeSystem.TryGetNode<AtmosPipeNodeComponent, DirNodeComponent>(uid, component.InletName, out var inletId, out var inletNode, out var inlet, out var inletDir)
            && _pipeNodeSystem.TryGetGas(inletId, out var inletGas, inlet, inletNode))
                gasMixDict.Add(Loc.GetString("gas-analyzer-window-text-inlet"), inletGas);
            if (_nodeSystem.TryGetNode<AtmosPipeNodeComponent, DirNodeComponent>(uid, component.FilterName, out var filterId, out var filterNode, out var filter, out var filterDir)
            && _pipeNodeSystem.TryGetGas(filterId, out var filterGas, filter, filterNode))
                gasMixDict.Add(Loc.GetString("gas-analyzer-window-text-filter"), filterGas);
            if (_nodeSystem.TryGetNode<AtmosPipeNodeComponent>(uid, component.OutletName, out var outletId, out var outletNode, out var outlet)
            && _pipeNodeSystem.TryGetGas(outletId, out var outletGas, outlet, outletNode))
                gasMixDict.Add(Loc.GetString("gas-analyzer-window-text-outlet"), outletGas);

            args.GasMixtures = gasMixDict;
            args.DeviceFlipped = inletDir != null && filterDir != null && inletDir.CurrentDirection.ToDirection() == filterDir.CurrentDirection.ToDirection().GetClockwise90Degrees();
        }
    }
}
