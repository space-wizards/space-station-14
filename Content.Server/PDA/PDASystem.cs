using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Instruments;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.PDA.Ringer;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Server.Mind.Components;
using Content.Server.Traitor;

namespace Content.Server.PDA
{
    public sealed class PDASystem : SharedPDASystem
    {
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;
        [Dependency] private readonly RingerSystem _ringerSystem = default!;
        [Dependency] private readonly InstrumentSystem _instrumentSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
        [Dependency] private readonly StoreSystem _storeSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, LightToggleEvent>(OnLightToggle);
            SubscribeLocalEvent<PDAComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
            SubscribeLocalEvent<PDAComponent, StoreAddedEvent>(OnUplinkInit);
            SubscribeLocalEvent<PDAComponent, StoreRemovedEvent>(OnUplinkRemoved);
            SubscribeLocalEvent<PDAComponent, GridModifiedEvent>(OnGridChanged);
        }

        protected override void OnComponentInit(EntityUid uid, PDAComponent pda, ComponentInit args)
        {
            base.OnComponentInit(uid, pda, args);

            if (!TryComp(uid, out ServerUserInterfaceComponent? uiComponent))
                return;

            UpdateStationName(pda);

            if (_uiSystem.TryGetUi(uid, PDAUiKey.Key, out var ui, uiComponent))
                ui.OnReceiveMessage += (msg) => OnUIMessage(pda, msg);
        }

        protected override void OnItemInserted(EntityUid uid, PDAComponent pda, EntInsertedIntoContainerMessage args)
        {
            base.OnItemInserted(uid, pda, args);
            UpdatePDAUserInterface(pda);
        }

        protected override void OnItemRemoved(EntityUid uid, PDAComponent pda, EntRemovedFromContainerMessage args)
        {
            base.OnItemRemoved(uid, pda, args);
            UpdatePDAUserInterface(pda);
        }

        private void OnLightToggle(EntityUid uid, PDAComponent pda, LightToggleEvent args)
        {
            pda.FlashlightOn = args.IsOn;
            UpdatePDAUserInterface(pda);
        }

        public void SetOwner(PDAComponent pda, string ownerName)
        {
            pda.OwnerName = ownerName;
            UpdatePDAUserInterface(pda);
        }

        private void OnUplinkInit(EntityUid uid, PDAComponent pda, StoreAddedEvent args)
        {
            UpdatePDAUserInterface(pda);
        }

        private void OnUplinkRemoved(EntityUid uid, PDAComponent pda, StoreRemovedEvent args)
        {
            UpdatePDAUserInterface(pda);
        }

        private void OnGridChanged(EntityUid uid, PDAComponent pda, GridModifiedEvent args)
        {
            UpdateStationName(pda);
            UpdatePDAUserInterface(pda);
        }

        private void UpdatePDAUserInterface(PDAComponent pda)
        {
            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = pda.OwnerName,
                IdOwner = pda.ContainedID?.FullName,
                JobTitle = pda.ContainedID?.JobTitle
            };

            if (!_uiSystem.TryGetUi(pda.Owner, PDAUiKey.Key, out var ui))
                return;

            var address = GetDeviceNetAddress(pda.Owner);
            var hasInstrument = HasComp<InstrumentComponent>(pda.Owner);
            var state = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, pda.StationName, false, hasInstrument, address);

            _cartridgeLoaderSystem?.UpdateUiState(pda.Owner, state);

            // TODO UPLINK RINGTONES/SECRETS This is just a janky placeholder way of hiding uplinks from non syndicate
            // players. This should really use a sort of key-code entry system that selects an account which is not directly tied to
            // a player entity.

            if (!TryComp<StoreComponent>(pda.Owner, out var storeComponent))
                return;

            var uplinkState = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, pda.StationName, true, hasInstrument, address);

            foreach (var session in ui.SubscribedSessions)
            {
                if (session.AttachedEntity is not { Valid: true } user)
                    continue;

                if (storeComponent.AccountOwner == user || (TryComp<MindComponent>(session.AttachedEntity, out var mindcomp) && mindcomp.Mind != null &&
                    mindcomp.Mind.HasRole<TraitorRole>()))
                    _cartridgeLoaderSystem?.UpdateUiState(pda.Owner, uplinkState, session);
            }
        }

        private void OnUIMessage(PDAComponent pda, ServerBoundUserInterfaceMessage msg)
        {
            // todo: move this to entity events
            switch (msg.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                    UpdatePDAUserInterface(pda);
                    break;
                case PDAToggleFlashlightMessage _:
                    {
                        if (EntityManager.TryGetComponent(pda.Owner, out UnpoweredFlashlightComponent? flashlight))
                            _unpoweredFlashlight.ToggleLight(flashlight.Owner, flashlight);
                        break;
                    }

                case PDAShowUplinkMessage _:
                    {
                        if (msg.Session.AttachedEntity != null &&
                            TryComp<StoreComponent>(pda.Owner, out var store))
                            _storeSystem.ToggleUi(msg.Session.AttachedEntity.Value, store);
                        break;
                    }
                case PDAShowRingtoneMessage _:
                    {
                        if (EntityManager.TryGetComponent(pda.Owner, out RingerComponent? ringer))
                            _ringerSystem.ToggleRingerUI(ringer, msg.Session);
                        break;
                    }
                case PDAShowMusicMessage _:
                {
                    if (TryComp(pda.Owner, out InstrumentComponent? instrument))
                        _instrumentSystem.ToggleInstrumentUi(pda.Owner, msg.Session, instrument);
                    break;
                }
            }
        }

        private void UpdateStationName(PDAComponent pda)
        {
            var station = _stationSystem.GetOwningStation(pda.Owner);
            pda.StationName = station is null ? null : Name(station.Value);
        }

        private void AfterUIOpen(EntityUid uid, PDAComponent pda, AfterActivatableUIOpenEvent args)
        {
            //TODO: this is awful
            // A new user opened the UI --> Check if they are a traitor and should get a user specific UI state override.
            if (!TryComp<StoreComponent>(pda.Owner, out var storeComp))
                return;

            if (storeComp.AccountOwner != args.User &&
                !(TryComp<MindComponent>(args.User, out var mindcomp) && mindcomp.Mind != null && mindcomp.Mind.HasRole<TraitorRole>()))
                return;

            if (!_uiSystem.TryGetUi(pda.Owner, PDAUiKey.Key, out var ui))
                return;

            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = pda.OwnerName,
                IdOwner = pda.ContainedID?.FullName,
                JobTitle = pda.ContainedID?.JobTitle
            };

            var state = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, pda.StationName, true, HasComp<InstrumentComponent>(pda.Owner), GetDeviceNetAddress(pda.Owner));

            _cartridgeLoaderSystem?.UpdateUiState(uid, state, args.Session);
        }

        private string? GetDeviceNetAddress(EntityUid uid)
        {
            string? address = null;

            if (TryComp(uid, out DeviceNetworkComponent? deviceNetworkComponent))
            {
                address = deviceNetworkComponent?.Address;
            }

            return address;
        }
    }
}
