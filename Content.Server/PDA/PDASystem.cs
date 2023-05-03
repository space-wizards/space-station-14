using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Instruments;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.PDA.Ringer;
using Content.Server.Station.Systems;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.UserInterface;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Shared.Light.Component;

namespace Content.Server.PDA
{
    public sealed class PDASystem : SharedPDASystem
    {
        [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
        [Dependency] private readonly InstrumentSystem _instrument = default!;
        [Dependency] private readonly RingerSystem _ringer = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly StoreSystem _store = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;

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

            UpdateStationName(uid, pda);

            if (_ui.TryGetUi(uid, PDAUiKey.Key, out var ui, uiComponent))
                ui.OnReceiveMessage += (msg) => OnUIMessage(pda, msg);
        }

        protected override void OnItemInserted(EntityUid uid, PDAComponent pda, EntInsertedIntoContainerMessage args)
        {
            base.OnItemInserted(uid, pda, args);
            UpdatePdaUi(uid, pda);
        }

        protected override void OnItemRemoved(EntityUid uid, PDAComponent pda, EntRemovedFromContainerMessage args)
        {
            base.OnItemRemoved(uid, pda, args);
            UpdatePdaUi(uid, pda);
        }

        private void OnLightToggle(EntityUid uid, PDAComponent pda, LightToggleEvent args)
        {
            pda.FlashlightOn = args.IsOn;
            UpdatePdaUi(uid, pda);
        }

        public void SetOwner(EntityUid uid, PDAComponent pda, string ownerName)
        {
            pda.OwnerName = ownerName;
            UpdatePdaUi(uid, pda);
        }

        private void OnGridChanged(EntityUid uid, PDAComponent pda, GridModifiedEvent args)
        {
            UpdateStationName(uid, pda);
            UpdatePdaUi(uid, pda);
        }

        /// <summary>
        /// Send new UI state to clients, call if you modify something like uplink.
        /// </summary>
        public void UpdatePdaUi(EntityUid uid, PDAComponent pda)
        {
            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = pda.OwnerName,
                IdOwner = pda.ContainedID?.FullName,
                JobTitle = pda.ContainedID?.JobTitle
            };

            if (!_ui.TryGetUi(uid, PDAUiKey.Key, out var ui))
                return;

            var address = GetDeviceNetAddress(uid);
            var hasInstrument = HasComp<InstrumentComponent>(uid);
            var showUplink = HasComp<StoreComponent>(uid) && IsUnlocked(uid);

            var state = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, pda.StationName, showUplink, hasInstrument, address);
            _cartridgeLoader?.UpdateUiState(uid, state);
        }

        private void OnUIMessage(PDAComponent pda, ServerBoundUserInterfaceMessage msg)
        {
            var uid = pda.Owner;
            // todo: move this to entity events
            switch (msg.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                    UpdatePdaUi(uid, pda);
                    break;
                case PDAToggleFlashlightMessage _:
                    {
                        if (TryComp<UnpoweredFlashlightComponent>(uid, out var flashlight))
                            _unpoweredFlashlight.ToggleLight(uid, flashlight);
                        break;
                    }
                case PDAShowRingtoneMessage _:
                    {
                        if (TryComp<RingerComponent>(uid, out var ringer))
                            _ringer.ToggleRingerUI(ringer, msg.Session);
                        break;
                    }
                case PDAShowMusicMessage _:
                {
                    if (TryComp<InstrumentComponent>(uid, out var instrument))
                        _instrument.ToggleInstrumentUi(uid, msg.Session, instrument);
                    break;
                }
                case PDAShowUplinkMessage _:
                {
                    // check if its locked again to prevent malicious clients opening locked uplinks
                    if (TryComp<StoreComponent>(uid, out var store) && IsUnlocked(uid))
                        _store.ToggleUi(msg.Session.AttachedEntity!.Value, uid, store);
                    break;
                }
                case PDALockUplinkMessage _:
                {
                    if (TryComp<RingerUplinkComponent>(uid, out var uplink))
                    {
                        _ringer.LockUplink(uid, uplink);
                        UpdatePdaUi(uid, pda);
                    }
                    break;
                }
            }
        }

        private bool IsUnlocked(EntityUid uid)
        {
            return TryComp<RingerUplinkComponent>(uid, out var uplink) ? uplink.Unlocked : true;
        }

        private void UpdateStationName(EntityUid uid, PDAComponent pda)
        {
            var station = _station.GetOwningStation(uid);
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
