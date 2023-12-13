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
    public sealed class GasFilterSystem : EntitySystem
    {
        [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

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

        private void OnFilterUpdated(EntityUid uid, GasFilterComponent filter, AtmosDeviceUpdateEvent args)
        {
            if (!filter.Enabled
                || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                || !EntityManager.TryGetComponent(uid, out AtmosDeviceComponent? device)
                || !_nodeContainer.TryGetNode(nodeContainer, filter.InletName, out PipeNode? inletNode)
                || !_nodeContainer.TryGetNode(nodeContainer, filter.FilterName, out PipeNode? filterNode)
                || !_nodeContainer.TryGetNode(nodeContainer, filter.OutletName, out PipeNode? outletNode)
                || outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure) // No need to transfer if target is full.
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var transferVol = filter.TransferRate * _atmosphereSystem.PumpSpeedup() * args.dt;

            if (transferVol <= 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var removed = inletNode.Air.RemoveVolume(transferVol);

            if (filter.FilteredGas.HasValue)
            {
                var filteredOut = new GasMixture() {Temperature = removed.Temperature};

                filteredOut.SetMoles(filter.FilteredGas.Value, removed.GetMoles(filter.FilteredGas.Value));
                removed.SetMoles(filter.FilteredGas.Value, 0f);

                var target = filterNode.Air.Pressure < Atmospherics.MaxOutputPressure ? filterNode : inletNode;
                _atmosphereSystem.Merge(target.Air, filteredOut);
                _ambientSoundSystem.SetAmbience(uid, filteredOut.TotalMoles > 0f);
            }

            _atmosphereSystem.Merge(outletNode.Air, removed);
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
            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            var gasMixDict = new Dictionary<string, GasMixture?>();

            if(_nodeContainer.TryGetNode(nodeContainer, component.InletName, out PipeNode? inlet))
                gasMixDict.Add(Loc.GetString("gas-analyzer-window-text-inlet"), inlet.Air);
            if(_nodeContainer.TryGetNode(nodeContainer, component.FilterName, out PipeNode? filterNode))
                gasMixDict.Add(Loc.GetString("gas-analyzer-window-text-filter"), filterNode.Air);
            if(_nodeContainer.TryGetNode(nodeContainer, component.OutletName, out PipeNode? outlet))
                gasMixDict.Add(Loc.GetString("gas-analyzer-window-text-outlet"), outlet.Air);

            args.GasMixtures = gasMixDict;
            args.DeviceFlipped = inlet != null && filterNode != null && inlet.CurrentPipeDirection.ToDirection() == filterNode.CurrentPipeDirection.ToDirection().GetClockwise90Degrees();
        }
    }
}
