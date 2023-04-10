using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.AlertLevel;
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
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Server.GameTicking;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;

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
        [Dependency] public readonly GameTicker GameTicker = default!;
        [Dependency] private readonly AccessSystem _access = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StationSystem _station = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, LightToggleEvent>(OnLightToggle);
            SubscribeLocalEvent<PDAComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
            SubscribeLocalEvent<PDAComponent, StoreAddedEvent>(OnUplinkInit);
            SubscribeLocalEvent<PDAComponent, StoreRemovedEvent>(OnUplinkRemoved);
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

        private void OnUplinkInit(EntityUid uid, PDAComponent pda, ref StoreAddedEvent args)
        {
            UpdatePDAUserInterface(pda);
        }

        private void OnUplinkRemoved(EntityUid uid, PDAComponent pda, ref StoreRemovedEvent args)
        {
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
            TimeSpan currentTime;
            currentTime = GameTicker.RoundDuration();

            var stationTime = new StationTimeText
            {
                Hours = currentTime.Hours.ToString(),
                Minutes = currentTime.Minutes.ToString(),
            };

            var stationUid = _stationSystem.GetOwningStation(pda.Owner);
            if (TryComp(stationUid, out AlertLevelComponent? alertComp) &&
                alertComp.AlertLevels != null)
            {
                pda.StationAlertLevel = alertComp.CurrentLevel;
                if (alertComp.AlertLevels.Levels.TryGetValue(alertComp.CurrentLevel, out var details))
                    pda.StationAlertColor = details.Color;
            }
            else
            {
                pda.StationAlertLevel = null;
                pda.StationAlertColor = Color.White;
            }

            List<string> accessLevels;
            if (pda.IdSlot.Item is { Valid: true } targetId)
            {
                var accessLevels_enum = _access.TryGetTags(targetId) ?? new List<string>();
                accessLevels = accessLevels_enum.ToList();
            }
            else
            {
                accessLevels = new List<string>();
            }

            var accessLevelsConvert = new List<string>();
            accessLevels.RemoveAll(s => s.Contains("EmergencyShuttleRepealAll"));
            foreach (var access in accessLevels)
            {
                if (!_prototypeManager.TryIndex<AccessLevelPrototype>(access, out var accessLevel))
                {
                    Logger.ErrorS(SharedIdCardConsoleSystem.Sawmill, $"Unable to find accesslevel for {access}");
                    continue;
                }

                accessLevelsConvert.Add(GetAccessLevelName(accessLevel));
            }

            UpdateStationName(pda);

            var state = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, accessLevelsConvert,
                stationTime, pda.StationName, false, hasInstrument,
                address, pda.StationAlertLevel, pda.StationAlertColor);

            _cartridgeLoaderSystem?.UpdateUiState(pda.Owner, state);

            // TODO UPLINK RINGTONES/SECRETS This is just a janky placeholder way of hiding uplinks from non syndicate
            // players. This should really use a sort of key-code entry system that selects an account which is not directly tied to
            // a player entity.

            if (!TryComp<StoreComponent>(pda.Owner, out var storeComponent))
                return;

            var uplinkState = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo,
                accessLevelsConvert, stationTime, pda.StationName, true, hasInstrument, address,
                state.StationAlertLevel);

            foreach (var session in ui.SubscribedSessions)
            {
                if (session.AttachedEntity is not { Valid: true } user)
                    continue;

                if (storeComponent.AccountOwner == user || (TryComp<MindComponent>(session.AttachedEntity, out var mindcomp) && mindcomp.Mind != null &&
                    mindcomp.Mind.HasRole<TraitorRole>()))
                    _cartridgeLoaderSystem?.UpdateUiState(pda.Owner, uplinkState, session);
            }
        }

        private static string GetAccessLevelName(AccessLevelPrototype prototype)
        {
            if (prototype.Name is { } name)
                return name;

            return prototype.ID;
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

                case PDAShowUplinkMessage _:
                    {
                        if (msg.Session.AttachedEntity != null &&
                            TryComp<StoreComponent>(pdaEnt, out var store))
                            _storeSystem.ToggleUi(msg.Session.AttachedEntity.Value, pdaEnt, store);
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
            if (TryComp(station, out MetaDataComponent? metaData))
            {
                pda.StationName = metaData.EntityName;
            }
            else
            {
                pda.StationName = null;
            }
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

            TimeSpan currentTime;
            currentTime = GameTicker.RoundDuration();
            var stationTime = new StationTimeText
            {
                Hours = currentTime.Hours.ToString(),
                Minutes = currentTime.Minutes.ToString(),
            };

            var stationUid = _stationSystem.GetOwningStation(pda.Owner);
            if (TryComp(stationUid, out AlertLevelComponent? alertComp) &&
                alertComp.AlertLevels != null)
            {
                pda.StationAlertLevel = alertComp.CurrentLevel;
                if (!TryComp<AlertLevelComponent>(stationUid, out var alerts))
                    return;
                if (alertComp.AlertLevels.Levels.TryGetValue(alerts.CurrentLevel, out var details))
                    pda.StationAlertColor = details.Color;
            }
            else
            {
                pda.StationAlertLevel = null;
                pda.StationAlertColor = Color.White;
            }

            List<string> accessLevels;
            if (pda.IdSlot.Item is { Valid: true } targetId)
            {
                var accessLevels_enum = _access.TryGetTags(targetId) ?? new List<string>();
                accessLevels = accessLevels_enum.ToList();
            }
            else
            {
                accessLevels = new List<string>();
            }

            List<string> accessLevelsConvert = new List<string>();
            foreach (var access in accessLevels)
            {
                if (!_prototypeManager.TryIndex<AccessLevelPrototype>(access, out var accessLevel))
                {
                    Logger.ErrorS(SharedIdCardConsoleSystem.Sawmill, $"Unable to find accesslevel for {access}");
                    continue;
                }

                accessLevelsConvert.Add(GetAccessLevelName(accessLevel));
            }

            UpdateStationName(pda);

            var state = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, accessLevelsConvert,
                stationTime, pda.StationName, true, HasComp<InstrumentComponent>(pda.Owner),
                GetDeviceNetAddress(pda.Owner), pda.StationAlertLevel, pda.StationAlertColor);

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
