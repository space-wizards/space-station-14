using Content.Server.AlertLevel;
using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Instruments;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.Mind;
using Content.Server.PDA.Ringer;
using Content.Server.Station.Systems;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Access.Components;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Content.Shared.Light.Component;
using Content.Server.MassMedia.Components;
using Content.Server.MassMedia.Systems;

namespace Content.Server.PDA
{
    public sealed class PdaSystem : SharedPdaSystem
    {
        [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
        [Dependency] private readonly InstrumentSystem _instrument = default!;
        [Dependency] private readonly RingerSystem _ringer = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly StoreSystem _store = default!;
        [Dependency] private readonly NewsSystem _news = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PdaComponent, LightToggleEvent>(OnLightToggle);

            // UI Events:
            SubscribeLocalEvent<PdaComponent, PdaRequestUpdateInterfaceMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaToggleFlashlightMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowRingtoneMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowMusicMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowUplinkMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaLockUplinkMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaOpenNewsMessage>(OnUiMessage);

            SubscribeLocalEvent<StationRenamedEvent>(OnStationRenamed);
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        }

        protected override void OnComponentInit(EntityUid uid, PdaComponent pda, ComponentInit args)
        {
            base.OnComponentInit(uid, pda, args);

            if (!HasComp<ServerUserInterfaceComponent>(uid))
                return;

            UpdateAlertLevel(uid, pda);
            UpdateStationName(uid, pda);
        }

        protected override void OnItemInserted(EntityUid uid, PdaComponent pda, EntInsertedIntoContainerMessage args)
        {
            base.OnItemInserted(uid, pda, args);
            UpdatePdaUi(uid, pda);
        }

        protected override void OnItemRemoved(EntityUid uid, PdaComponent pda, EntRemovedFromContainerMessage args)
        {
            base.OnItemRemoved(uid, pda, args);
            UpdatePdaUi(uid, pda);
        }

        private void OnLightToggle(EntityUid uid, PdaComponent pda, LightToggleEvent args)
        {
            pda.FlashlightOn = args.IsOn;
            UpdatePdaUi(uid, pda);
        }

        public void SetOwner(EntityUid uid, PdaComponent pda, string ownerName)
        {
            pda.OwnerName = ownerName;
            UpdatePdaUi(uid, pda);
        }

        private void OnStationRenamed(StationRenamedEvent ev)
        {
            UpdateAllPdaUisOnStation();
        }

        private void OnAlertLevelChanged(AlertLevelChangedEvent args)
        {
            UpdateAllPdaUisOnStation();
        }

        private void UpdateAllPdaUisOnStation()
        {
            var query = EntityQueryEnumerator<PdaComponent>();
            while (query.MoveNext(out var ent, out var comp))
            {
                UpdatePdaUi(ent, comp);
            }
        }

        /// <summary>
        /// Send new UI state to clients, call if you modify something like uplink.
        /// </summary>
        public void UpdatePdaUi(EntityUid uid, PdaComponent pda)
        {
            if (!_ui.TryGetUi(uid, PdaUiKey.Key, out _))
                return;

            var address = GetDeviceNetAddress(uid);
            var hasInstrument = HasComp<InstrumentComponent>(uid);
            var showUplink = HasComp<StoreComponent>(uid) && IsUnlocked(uid);
            var showNews = HasComp<NewsReadComponent>(uid);

            UpdateStationName(uid, pda);
            UpdateAlertLevel(uid, pda);
            // TODO: Update the level and name of the station with each call to UpdatePdaUi is only needed for latejoin players.
            // TODO: If someone can implement changing the level and name of the station when changing the PDA grid, this can be removed.

            var id = CompOrNull<IdCardComponent>(pda.ContainedId);
            var state = new PdaUpdateState(
                pda.FlashlightOn,
                pda.PenSlot.HasItem,
                new PdaIdInfoText
                {
                    ActualOwnerName = pda.OwnerName,
                    IdOwner = id?.FullName,
                    JobTitle = id?.JobTitle,
                    StationAlertLevel = pda.StationAlertLevel,
                    StationAlertColor = pda.StationAlertColor
                },
                pda.StationName,
                showUplink,
                hasInstrument,
                showNews,
                address);

            _cartridgeLoader?.UpdateUiState(uid, state);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaRequestUpdateInterfaceMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            UpdatePdaUi(uid, pda);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaToggleFlashlightMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<UnpoweredFlashlightComponent>(uid, out var flashlight))
                _unpoweredFlashlight.ToggleLight(uid, flashlight);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowRingtoneMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (HasComp<RingerComponent>(uid))
                _ringer.ToggleRingerUI(uid, (IPlayerSession) msg.Session);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowMusicMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<InstrumentComponent>(uid, out var instrument))
                _instrument.ToggleInstrumentUi(uid, (IPlayerSession) msg.Session, instrument);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowUplinkMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            // check if its locked again to prevent malicious clients opening locked uplinks
            if (TryComp<StoreComponent>(uid, out var store) && IsUnlocked(uid))
                _store.ToggleUi(msg.Session.AttachedEntity!.Value, uid, store);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaLockUplinkMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<RingerUplinkComponent>(uid, out var uplink))
            {
                _ringer.LockUplink(uid, uplink);
                UpdatePdaUi(uid, pda);
            }
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaOpenNewsMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<NewsReadComponent>(uid, out var news))
            {
                _news.ToggleUi(msg.Session.AttachedEntity!.Value, uid, news);
                UpdatePdaUi(uid, pda);
            }
        }

        private bool IsUnlocked(EntityUid uid)
        {
            return !TryComp<RingerUplinkComponent>(uid, out var uplink) || uplink.Unlocked;
        }

        private void UpdateStationName(EntityUid uid, PdaComponent pda)
        {
            var station = _station.GetOwningStation(uid);
            pda.StationName = station is null ? null : Name(station.Value);
        }

        private void UpdateAlertLevel(EntityUid uid, PdaComponent pda)
        {
            var station = _station.GetOwningStation(uid);
            if (!TryComp(station, out AlertLevelComponent? alertComp) ||
                alertComp.AlertLevels == null)
                return;
            pda.StationAlertLevel = alertComp.CurrentLevel;
            if (alertComp.AlertLevels.Levels.TryGetValue(alertComp.CurrentLevel, out var details))
                pda.StationAlertColor = details.Color;
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
