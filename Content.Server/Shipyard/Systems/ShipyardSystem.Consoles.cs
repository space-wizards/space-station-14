using Content.Server.Popups;
using Content.Server.Cargo.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shipyard.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Shipyard.Events;
using Content.Shared.Shipyard.BUI;
using Content.Shared.Shipyard.Prototypes;
using Content.Shared.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Shipyard;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;

namespace Content.Server.Shipyard.Systems
{
    public sealed class ShipyardConsoleSystem : SharedShipyardSystem
    {
        [Dependency] private readonly AccessReaderSystem _access = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ShipyardSystem _shipyard = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly CargoSystem _cargo = default!;
        [Dependency] private readonly RadioSystem _radio = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public void InitializeConsole()
        {
            SubscribeLocalEvent<ShipyardConsoleComponent, ShipyardConsolePurchaseMessage>(OnPurchaseMessage);
            SubscribeLocalEvent<ShipyardConsoleComponent, BoundUIOpenedEvent>(OnConsoleUIOpened);
            //SubscribeLocalEvent<ShipyardConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

        private void OnInit(EntityUid uid, SharedShipyardConsoleComponent orderConsole, ComponentInit args)
        {
            //_shipyard.SetupShipyard(); ///if we have to start up the shipyard from here later
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            //_shipyard.Shutdown(); //round cleanup event in case of needing OnInit;
        }

        private void OnPurchaseMessage(EntityUid uid, SharedShipyardConsoleComponent component, ShipyardConsolePurchaseMessage args)
        {
            if (args.Session.AttachedEntity is not { Valid : true } player)
            {
                return;
            }

            if (TryComp<AccessReaderComponent>(uid, out var accessReaderComponent) && accessReaderComponent.Enabled && !_access.IsAllowed(player, accessReaderComponent))
            {
                ConsolePopup(args.Session, Loc.GetString("comms-console-permission-denied"));
                PlayDenySound(uid, component);
                return;
            }

            VesselPrototype? vessel = null;

            if (!_prototypeManager.TryIndex<VesselPrototype>(args.Vessel, out vessel) || vessel == null)
            {
                ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.Vessel)));
                PlayDenySound(uid, component);
                return;
            }

            if (vessel.Price <= 0)
                return;

            var station = _station.GetOwningStation(uid);
            var bank = GetBankAccount(station);

            if (bank == null)
                return;

            if (bank.Balance <= vessel.Price)
            {
                ConsolePopup(args.Session, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
                PlayDenySound(uid, component);
                return;
            }

            if (!TryPurchaseVessel(bank, vessel, out var shuttle) || shuttle == null)
            {
                PlayDenySound(uid, component);
                return;
            }

            _cargo.DeductFunds(bank, vessel.Price);
            var channel = _prototypeManager.Index<RadioChannelPrototype>("Command");
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking", ("vessel", vessel.Name.ToString())), channel);
            PlayConfirmSound(uid, component);

            var newState = new ShipyardConsoleInterfaceState(
                bank.Balance,
                true);

            _ui.TrySetUiState(uid, ShipyardConsoleUiKey.Shipyard, newState); 
        }
        
        private void OnConsoleUIOpened(EntityUid uid, SharedShipyardConsoleComponent component, BoundUIOpenedEvent args)
        {
            if (!args.Session.AttachedEntity.HasValue)
                return;

            var station = _station.GetOwningStation(uid);
            var bank = GetBankAccount(station);

            if (bank == null)
                return;

            var newState = new ShipyardConsoleInterfaceState(
                bank.Balance,
                true);

            _ui.TrySetUiState(uid, ShipyardConsoleUiKey.Shipyard, newState);
        }

        private void ConsolePopup(ICommonSession session, string text)
        {
            if (session.AttachedEntity is { Valid : true } player)
                _popup.PopupEntity(text, player);
        }

        private void PlayDenySound(EntityUid uid, SharedShipyardConsoleComponent component)
        {
            _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
        }
        
        private void PlayConfirmSound(EntityUid uid, SharedShipyardConsoleComponent component)
        {
            _audio.PlayPvs(_audio.GetSound(component.ConfirmSound), uid);
        }

        private bool TryPurchaseVessel(StationBankAccountComponent component, VesselPrototype vessel, out ShuttleComponent? deed)
        {
            var stationUid = _station.GetOwningStation(component.Owner);

            if (component == null || vessel == null || vessel.ShuttlePath == null || stationUid == null)
            {
                deed = null;
                return false;
            };

            _shipyard.PurchaseShuttle(stationUid, vessel.ShuttlePath.ToString(), out deed);

            if (deed == null)
            {
                return false;
            };

            return true;
        }
        
        public StationBankAccountComponent? GetBankAccount(EntityUid? uid)
        {
            if (uid != null && TryComp<StationBankAccountComponent>(uid, out var bankAccount))
            { 
                return bankAccount;
            }    
            return null;
        }
    }
}
