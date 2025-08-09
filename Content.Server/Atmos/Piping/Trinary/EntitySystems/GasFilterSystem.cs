using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Components;
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
            SubscribeLocalEvent<GasFilterComponent, ActivateInWorldEvent>(OnFilterActivate);
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

        private void OnFilterUpdated(EntityUid uid, GasFilterComponent filter, ref AtmosDeviceUpdateEvent args)
        {
            // STARLIGHT - Disable outlet node pressure check for inline filter
            if (!filter.Enabled
                || !_nodeContainer.TryGetNodes(uid, filter.InletName, filter.FilterName, filter.OutletName, out PipeNode? inletNode, out PipeNode? filterNode, out PipeNode? outletNode)
                || (outletNode != inletNode && outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure)) // No need to transfer if target is full.
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }
            
            //starlight edit - Moved logic to a new method
            var transferVol = GetTransferRate(filter, args, inletNode.Air, outletNode); //starlight edit

            if (transferVol <= 0)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var removed = inletNode.Air.RemoveVolume(transferVol);

            if (filter.FilteredGas.HasValue)
            {
                var wantsToFilter = new GasMixture(removed.Volume) { Temperature = removed.Temperature };

                wantsToFilter.SetMoles(filter.FilteredGas.Value, removed.GetMoles(filter.FilteredGas.Value));
                removed.SetMoles(filter.FilteredGas.Value, 0f);
                
                // starlight edit start - fix subtick
                var filterVolume = GetTransferRate(filter, args, wantsToFilter, filterNode);
                
                // Remove the filtered volume that actually can fit in the filter
                var actuallyFiltered = wantsToFilter.RemoveVolume(filterVolume);
                
                // The remaining gas in wantsToFilter should be returned to inlet
                var returned = wantsToFilter;
                
                // Put gases in their respective nodes
                _atmosphereSystem.Merge(filterNode.Air, actuallyFiltered);
                _atmosphereSystem.Merge(inletNode.Air, returned);
                // starlight edit end - fix subtick
                
                _ambientSoundSystem.SetAmbience(uid, wantsToFilter.TotalMoles > 0f); // starlight edit - fix subtick
            }

            _atmosphereSystem.Merge(outletNode.Air, removed);
        }

        
        //starlight fix subtick
        /// <summary>
        /// Calculates how many moles of gas to transfer from the inlet to the outlet.
        /// </summary>
        /// <param name="filter">A filter component</param>
        /// <param name="args">Arguments of the event</param>
        /// <param name="inletGasMixture">Gas mixture in the inlet node (simplified for easier use)</param>
        /// <param name="outletNode">Output for the gas</param>
        /// <returns>Returns the flow rate in volume(L/s) of how much gas has to be moved to fill the outlet</returns>
        private float GetTransferRate(GasFilterComponent filter, AtmosDeviceUpdateEvent args, GasMixture inletGasMixture,
            PipeNode outletNode)
        {
            float wantToTransfer = filter.TransferRate * _atmosphereSystem.PumpSpeedup() * args.dt;

            // Get The Volume to transfer, do not attempt to transfer more than the pipe can hold.
            float transferVolume = Math.Min(inletGasMixture.Volume, wantToTransfer);

            // Calculate how many moles does this transfer contain
            float transferMoles =
                inletGasMixture.Pressure * transferVolume / (inletGasMixture.Temperature * Atmospherics.R);

            // Calculate how many moles can outlet still contain
            float molesSpaceLeft = (Atmospherics.MaxOutputPressure - outletNode.Air.Pressure) * outletNode.Air.Volume /
                                   (outletNode.Air.Temperature * Atmospherics.R);

            // Get the lower value of the two, and clamp it to the transfer rate
            float actualMolesTransfered = Math.Clamp(transferMoles, 0, Math.Max(0, molesSpaceLeft));

            float actualTransferVolume = 0;
            if (actualMolesTransfered > 0 && inletGasMixture.Pressure > 0)
            {
                // Calculate how much volume is needed to transfer those moles
                actualTransferVolume = actualMolesTransfered * inletGasMixture.Temperature * Atmospherics.R /
                                       inletGasMixture.Pressure;
            }

            return actualTransferVolume;
        }
        //starlight end

        private void OnFilterLeaveAtmosphere(EntityUid uid, GasFilterComponent filter, ref AtmosDeviceDisabledEvent args)
        {
            filter.Enabled = false;

            UpdateAppearance(uid, filter);
            _ambientSoundSystem.SetAmbience(uid, false);

            DirtyUI(uid, filter);
            _userInterfaceSystem.CloseUi(uid, GasFilterUiKey.Key);
        }

        private void OnFilterActivate(EntityUid uid, GasFilterComponent filter, ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (!TryComp(args.User, out ActorComponent? actor))
                return;

            if (Comp<TransformComponent>(uid).Anchored)
            {
                _userInterfaceSystem.OpenUi(uid, GasFilterUiKey.Key, actor.PlayerSession);
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

            _userInterfaceSystem.SetUiState(uid, GasFilterUiKey.Key,
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
                $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
            DirtyUI(uid, filter);
            UpdateAppearance(uid, filter);
        }

        private void OnTransferRateChangeMessage(EntityUid uid, GasFilterComponent filter, GasFilterChangeRateMessage args)
        {
            filter.TransferRate = Math.Clamp(args.Rate, 0f, filter.MaxTransferRate);
            _adminLogger.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} set the transfer rate on {ToPrettyString(uid):device} to {args.Rate}");
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
                        $"{ToPrettyString(args.Actor):player} set the filter on {ToPrettyString(uid):device} to {parsedGas.ToString()}");
                    DirtyUI(uid, filter);
                }
                else
                {
                    Log.Warning($"{ToPrettyString(uid)} received GasFilterSelectGasMessage with an invalid ID: {args.ID}");
                }
            }
            else
            {
                filter.FilteredGas = null;
                _adminLogger.Add(LogType.AtmosFilterChanged, LogImpact.Medium,
                    $"{ToPrettyString(args.Actor):player} set the filter on {ToPrettyString(uid):device} to none");
                DirtyUI(uid, filter);
            }
        }

        /// <summary>
        /// Returns the gas mixture for the gas analyzer
        /// </summary>
        private void OnFilterAnalyzed(EntityUid uid, GasFilterComponent component, GasAnalyzerScanEvent args)
        {
            args.GasMixtures ??= new List<(string, GasMixture?)>();

            // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
            if (_nodeContainer.TryGetNode(uid, component.InletName, out PipeNode? inlet) && inlet.Air.Volume != 0f)
            {
                var inletAirLocal = inlet.Air.Clone();
                inletAirLocal.Multiply(inlet.Volume / inlet.Air.Volume);
                inletAirLocal.Volume = inlet.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-inlet"), inletAirLocal));
            }
            if (_nodeContainer.TryGetNode(uid, component.FilterName, out PipeNode? filterNode) && filterNode.Air.Volume != 0f)
            {
                var filterNodeAirLocal = filterNode.Air.Clone();
                filterNodeAirLocal.Multiply(filterNode.Volume / filterNode.Air.Volume);
                filterNodeAirLocal.Volume = filterNode.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-filter"), filterNodeAirLocal));
            }
            if (_nodeContainer.TryGetNode(uid, component.OutletName, out PipeNode? outlet) && outlet.Air.Volume != 0f)
            {
                var outletAirLocal = outlet.Air.Clone();
                outletAirLocal.Multiply(outlet.Volume / outlet.Air.Volume);
                outletAirLocal.Volume = outlet.Volume;
                args.GasMixtures.Add((Loc.GetString("gas-analyzer-window-text-outlet"), outletAirLocal));
            }

            // STARLIGHT START
            // if inlet and outlet are the same you cant get a direction from it
            if (inlet == outlet)
                return;
            // STARLIGHT END

            args.DeviceFlipped = inlet != null && filterNode != null && inlet.CurrentPipeDirection.ToDirection() == filterNode.CurrentPipeDirection.ToDirection().GetClockwise90Degrees();
        }
    }
}