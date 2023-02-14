using Content.Server.Popups;
using Content.Server.Cargo.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared.Shipyard.Events;
using Content.Shared.Shipyard.BUI;
using Content.Shared.Shipyard.Prototypes;
using Content.Shared.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Shipyard;
using Robust.Server.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Shipyard.Systems;

public sealed class ShipyardConsoleSystem : EntitySystem
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
    }

    private void OnPurchaseMessage(EntityUid uid, ShipyardConsoleComponent component, ShipyardConsolePurchaseMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid : true } player)
        {
            return;
        }

        if (TryComp<AccessReaderComponent>(uid, out var accessReaderComponent) && !_access.IsAllowed(player, accessReaderComponent))
        {
            ConsolePopup(args.Session, Loc.GetString("comms-console-permission-denied"));
            PlayDenySound(uid, component);
            return;
        }

        if (!_prototypeManager.TryIndex<VesselPrototype>(args.Vessel, out var vessel))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.Vessel)));
            PlayDenySound(uid, component);
            return;
        }

        if (vessel.Price <= 0)
            return;

        var station = _station.GetOwningStation(uid);

        if (!TryComp<StationBankAccountComponent>(station, out var bank))
        {
            PlayDenySound(uid, component);
            return;
        }

        if (bank.Balance <= vessel.Price)
        {
            ConsolePopup(args.Session, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
            PlayDenySound(uid, component);
            return;
        }

        if (!_shipyard.TryPurchaseShuttle((EntityUid) station, vessel.ShuttlePath.ToString(), out var shuttle))
        {
            PlayDenySound(uid, component);
            return;
        }

        _cargo.DeductFunds(bank, vessel.Price);
        var channel = _prototypeManager.Index<RadioChannelPrototype>(component.ShipyardChannel);
        _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking", ("vessel", vessel.Name.ToString())), channel);
        PlayConfirmSound(uid, component);
        RefreshState(uid, bank.Balance, true);
    }
    
    private void OnConsoleUIOpened(EntityUid uid, ShipyardConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!args.Session.AttachedEntity.HasValue)
            return;

        var station = _station.GetOwningStation(uid);

        if (!TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        RefreshState(uid, bank.Balance, true);
    }

    private void ConsolePopup(ICommonSession session, string text)
    {
        if (session.AttachedEntity is { Valid : true } player)
            _popup.PopupEntity(text, player);
    }

    private void PlayDenySound(EntityUid uid, ShipyardConsoleComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
    }
    
    private void PlayConfirmSound(EntityUid uid, ShipyardConsoleComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ConfirmSound), uid);
    }

    private void RefreshState(EntityUid uid, int balance, bool access)
    {
        var newState = new ShipyardConsoleInterfaceState(
            balance,
            access);

        _ui.TrySetUiState(uid, ShipyardConsoleUiKey.Shipyard, newState);
    }
}
