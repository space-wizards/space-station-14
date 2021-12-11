using System;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.UserInterface;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Trinary.EntitySystems
{
    [UsedImplicitly]
    public class GasFilterSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private AdminLogSystem _adminLogSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasFilterComponent, AtmosDeviceUpdateEvent>(OnFilterUpdated);
            SubscribeLocalEvent<GasFilterComponent, InteractHandEvent>(OnFilterInteractHand);
            // Bound UI subscriptions
            SubscribeLocalEvent<GasFilterComponent, GasFilterChangeRateMessage>(OnTransferRateChangeMessage);
            SubscribeLocalEvent<GasFilterComponent, GasFilterSelectGasMessage>(OnSelectGasMessage);
            SubscribeLocalEvent<GasFilterComponent, GasFilterToggleStatusMessage>(OnToggleStatusMessage);
        }

        private void OnFilterUpdated(EntityUid uid, GasFilterComponent filter, AtmosDeviceUpdateEvent args)
        {
            var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(filter.Owner);

            if (!filter.Enabled
            || !EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
            || !EntityManager.TryGetComponent(uid, out AtmosDeviceComponent? device)
            || !nodeContainer.TryGetNode(filter.InletName, out PipeNode? inletNode)
            || !nodeContainer.TryGetNode(filter.FilterName, out PipeNode? filterNode)
            || !nodeContainer.TryGetNode(filter.OutletName, out PipeNode? outletNode)
            || outletNode.Air.Pressure >= Atmospherics.MaxOutputPressure) // No need to transfer if target is full.
            {
                appearance?.SetData(FilterVisuals.Enabled, false);
                return;
            }

            // We multiply the transfer rate in L/s by the seconds passed since the last process to get the liters.
            var transferRatio = (float)(filter.TransferRate * (_gameTiming.CurTime - device.LastProcess).TotalSeconds) / inletNode.Air.Volume;

            if (transferRatio <= 0)
            {
                appearance?.SetData(FilterVisuals.Enabled, false);
                return;
            }

            var removed = inletNode.Air.RemoveRatio(transferRatio);

            if (filter.FilteredGas.HasValue)
            {
                appearance?.SetData(FilterVisuals.Enabled, true);

                var filteredOut = new GasMixture() {Temperature = removed.Temperature};

                filteredOut.SetMoles(filter.FilteredGas.Value, removed.GetMoles(filter.FilteredGas.Value));
                removed.SetMoles(filter.FilteredGas.Value, 0f);

                var target = filterNode.Air.Pressure < Atmospherics.MaxOutputPressure ? filterNode : inletNode;
                target.AssumeAir(filteredOut);
            }

            outletNode.AssumeAir(removed);
        }

        private void OnFilterInteractHand(EntityUid uid, GasFilterComponent component, InteractHandEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            if (EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored)
            {
                _userInterfaceSystem.TryOpen(uid, GasFilterUiKey.Key, actor.PlayerSession);
                DirtyUI(uid, component);
            }
            else
            {
                args.User.PopupMessageCursor(Loc.GetString("comp-gas-filter-ui-needs-anchor"));
            }

            args.Handled = true;
        }

        private void DirtyUI(EntityUid uid, GasFilterComponent? filter)
        {

            if (!Resolve(uid, ref filter))
                return;

            _userInterfaceSystem.TrySetUiState(uid, GasFilterUiKey.Key,
                new GasFilterBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(filter.Owner).EntityName, filter.TransferRate, filter.Enabled, filter.FilteredGas));
        }

        private void OnToggleStatusMessage(EntityUid uid, GasFilterComponent filter, GasFilterToggleStatusMessage args)
        {
            filter.Enabled = args.Enabled;
            _adminLogSystem.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");

            DirtyUI(uid, filter);
        }

        private void OnTransferRateChangeMessage(EntityUid uid, GasFilterComponent filter, GasFilterChangeRateMessage args)
        {
            filter.TransferRate = Math.Clamp(args.Rate, 0f, Atmospherics.MaxTransferRate);
            _adminLogSystem.Add(LogType.AtmosVolumeChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Session.AttachedEntity!.Value):player} set the transfer rate on {ToPrettyString(uid):device} to {args.Rate}");
            DirtyUI(uid, filter);

        }

        private void OnSelectGasMessage(EntityUid uid, GasFilterComponent filter, GasFilterSelectGasMessage args)
        {
            if (Enum.TryParse<Gas>(args.ID.ToString(), true, out var parsedGas))
            {
                filter.FilteredGas = parsedGas;
                DirtyUI(uid, filter);
            }

        }
    }
}
