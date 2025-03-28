using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._DV.Shipyard;
using Content.Shared._DV.Shipyard.Prototypes;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Content.Server._DV.Shipyard;

public sealed class ShipyardConsoleSystem : SharedShipyardConsoleSystem
{
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ShipyardSystem _shipyard = default!;
    [Dependency] private readonly StationSystem _station = default!;
    public override void Initialize()
    {
        base.Initialize();
        Subs.BuiEvents<ShipyardConsoleComponent>(ShipyardConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
        });
    }

    protected override void TryPurchase(Entity<ShipyardConsoleComponent> ent, EntityUid user, VesselPrototype vessel)
    {
        // client prevents asking for this so dont need feedback for validation
        if (_whitelist.IsWhitelistFail(vessel.Whitelist, ent))
            return;

        if (GetBankAccount(ent) is not {} bank)
            return;

        if (bank.Comp.Balance < vessel.Price)
        {
            var popup = Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price));
            Popup.PopupEntity(popup, ent, user);
            Audio.PlayPvs(ent.Comp.DenySound, ent);
            return;
        }

        if (_shipyard.TrySendShuttle(bank.Owner, GetResPath(vessel)) is not {} shuttle)
        {
            var popup = Loc.GetString("shipyard-console-error");
            Popup.PopupEntity(popup, ent, user);
            Audio.PlayPvs(ent.Comp.DenySound, ent);
            return;
        }

        _meta.SetEntityName(shuttle, $"{vessel.Name} {_random.Next(1000):000}");

        _cargo.UpdateBankAccount(bank.Owner, -vessel.Price);

        var message = Loc.GetString("shipyard-console-docking", ("vessel", vessel.Name.ToString()));
        _radio.SendRadioMessage(ent, message, ent.Comp.Channel, ent);
        Audio.PlayPvs(ent.Comp.ConfirmSound, ent);
        UpdateUI(ent, bank.Comp.Balance);
    }

    private void OnOpened(Entity<ShipyardConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUI(ent);
    }

    private void UpdateUI(EntityUid uid)
    {
        if (GetBankAccount(uid) is {} bank)
            UpdateUI(uid, bank.Comp.Balance);
    }

    private void UpdateUI(EntityUid uid, int balance)
    {
        if (!_shipyard.Enabled)
            return;

        var state = new ShipyardConsoleState(balance);
        _ui.SetUiState(uid, ShipyardConsoleUiKey.Key, state);
    }

    private Entity<StationBankAccountComponent>? GetBankAccount(EntityUid console)
    {
        if (_station.GetOwningStation(console) is not {} station)
            return null;

        if (!TryComp<StationBankAccountComponent>(station, out var bank))
            return null;

        return (station, bank);
    }

    private ResPath GetResPath(VesselPrototype vessel)
    {
        int randomPath = _random.Next(vessel.Path.Count);
        return vessel.Path[randomPath];
    }
}
