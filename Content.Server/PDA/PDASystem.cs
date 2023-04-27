using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Instruments;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.PDA.Ringer;
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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, LightToggleEvent>(OnLightToggle);
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
        }

        private void OnUIMessage(PDAComponent pda, ServerBoundUserInterfaceMessage msg)
        {
            var pdaEnt = pda.Owner;
            // todo: move this to entity events
            switch (msg.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                    UpdatePDAUserInterface(pda);
                    break;
                case PDAToggleFlashlightMessage _:
                    {
                        if (EntityManager.TryGetComponent(pdaEnt, out UnpoweredFlashlightComponent? flashlight))
                            _unpoweredFlashlight.ToggleLight(pdaEnt, flashlight);
                        break;
                    }
                case PDAShowRingtoneMessage _:
                    {
                        if (EntityManager.TryGetComponent(pdaEnt, out RingerComponent? ringer))
                            _ringerSystem.ToggleRingerUI(ringer, msg.Session);
                        break;
                    }
                case PDAShowMusicMessage _:
                {
                    if (TryComp(pdaEnt, out InstrumentComponent? instrument))
                        _instrumentSystem.ToggleInstrumentUi(pdaEnt, msg.Session, instrument);
                    break;
                }
            }
        }

        private void UpdateStationName(PDAComponent pda)
        {
            var station = _stationSystem.GetOwningStation(pda.Owner);
            pda.StationName = station is null ? null : Name(station.Value);
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
