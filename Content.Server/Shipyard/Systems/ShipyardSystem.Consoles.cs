using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Bank;
using Content.Shared.Bank.Components;
using Content.Shared.Shipyard.Events;
using Content.Shared.Shipyard.BUI;
using Content.Shared.Shipyard.Prototypes;
using Content.Shared.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Shipyard;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Internal.TypeSystem;
using Content.Server.Database;

namespace Content.Server.Shipyard.Systems;

public sealed partial class ShipyardSystem : SharedShipyardSystem
{
    [Dependency] private readonly AccessSystem _accessSystem = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly IdCardSystem _idSystem = default!;

    public void InitializeConsole()
    {

    }

    private void OnPurchaseMessage(EntityUid uid, ShipyardConsoleComponent component, ShipyardConsolePurchaseMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid : true } player)
        {
            return;
        }

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid : true } targetId)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<IdCardComponent>(targetId, out var idCard))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(uid, component);
            return;
        }

        if (HasComp<ShuttleDeedComponent>(targetId))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-already-deeded"));
            PlayDenySound(uid, component);
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

        if (_station.GetOwningStation(uid) is not { Valid : true } station)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-station"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-bank"));
            PlayDenySound(uid, component);
            return;
        }

        if (bank.Balance <= vessel.Price)
        {
            ConsolePopup(args.Session, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
            PlayDenySound(uid, component);
            return;
        }

        if (!_bank.TryBankWithdraw(player, vessel.Price))
        {
            ConsolePopup(args.Session, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryPurchaseShuttle((EntityUid) station, vessel.ShuttlePath.ToString(), out var shuttle))
        {
            PlayDenySound(uid, component);
            return;
        }

        string? deedName = null;

        if (TryComp<AccessComponent>(targetId, out var newCap))
        {
            //later we will make a custom pilot job, for now they get the captain treatment
            var newAccess = newCap.Tags.ToList();
            newAccess.Add($"Captain");
            _accessSystem.TrySetTags(targetId, newAccess, newCap);
            if (TryComp<MetaDataComponent>(shuttle.Owner, out var mData))
            {
                deedName = mData.EntityName;
            }
        }
        var newDeed = EnsureComp<ShuttleDeedComponent>(targetId);
        var channel = _prototypeManager.Index<RadioChannelPrototype>(component.ShipyardChannel);
        newDeed.ShuttleUid = shuttle.Owner;
        newDeed.ShuttleName = deedName;
        _idSystem.TryChangeJobTitle(targetId, $"Captain", idCard, player);
        _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking", ("vessel", vessel.Name.ToString())), channel);
        PlayConfirmSound(uid, component);
        RefreshState(uid, bank.Balance, true, deedName, true);
    }

    public void OnSellMessage(EntityUid uid, ShipyardConsoleComponent component, ShipyardConsoleSellMessage args)
    {

        if (args.Session.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<IdCardComponent>(targetId, out var idCard))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<ShuttleDeedComponent>(targetId, out var deed) || deed.ShuttleUid is not { Valid : true } shuttleUid)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-deed"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-bank"));
            PlayDenySound(uid, component);
            return;
        }

        if (_station.GetOwningStation(uid) is not { Valid : true } stationUid)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-station"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TrySellShuttle(stationUid, shuttleUid, out var bill))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-sale-reqs"));
            PlayDenySound(uid, component);
            return;
        };

        RemComp<ShuttleDeedComponent>(targetId);
        _bank.TryBankDeposit(player, bill);
        PlayConfirmSound(uid, component);
        RefreshState(uid, bank.Balance, true, null, true);
    }

    private void OnConsoleUIOpened(EntityUid uid, ShipyardConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        //      mayhaps re-enable this later for HoS/SA
        //        var station = _station.GetOwningStation(uid);

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            return;
        }
        var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;

        TryComp<ShuttleDeedComponent>(targetId, out var deed);

        RefreshState(uid, bank.Balance, true, deed?.ShuttleName, targetId.HasValue);
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

    private void OnItemSlotChanged(EntityUid uid, ShipyardConsoleComponent component, ContainerModifiedMessage args)
    {
        // kind of cursed. We need to update the UI when an Id is entered, but the UI needs to know the player characters bank account.
        var shipyardUi = _ui.GetUi(uid, ShipyardConsoleUiKey.Shipyard);
        var uiUser = shipyardUi.SubscribedSessions.FirstOrDefault();
        
        if (uiUser?.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            return;
        }

        var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;
        TryComp<ShuttleDeedComponent>(targetId, out var deed);
        RefreshState(uid, bank.Balance, true, deed?.ShuttleName, targetId.HasValue);
    }

    private void RefreshState(EntityUid uid, int balance, bool access, string? shipDeed, bool isTargetIdPresent)
    {
        var newState = new ShipyardConsoleInterfaceState(
            balance,
            access,
            shipDeed,
            isTargetIdPresent);

        _ui.TrySetUiState(uid, ShipyardConsoleUiKey.Shipyard, newState);
    }
}
