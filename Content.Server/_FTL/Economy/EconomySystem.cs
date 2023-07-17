using Content.Shared._FTL.Economy;
using Content.Server.Administration;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._FTL.Economy;

/// <summary>
/// This handles the withdrawal and deposit of credits into the cargo system
/// </summary>
public sealed class EconomySystem : SharedEconomySystem
{
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<OutpostATMComponent,GetVerbsEvent<ActivationVerb>>(OnOutpostATMGetVerbs);
        SubscribeLocalEvent<CreditComponent,AfterInteractEvent>(OnOutpostATMAfterInteract);
        SubscribeLocalEvent<OutpostATMComponent, ExaminedEvent>(OnOutpostATMExaminedEvent);
        SubscribeLocalEvent<OutpostATMComponent, PowerChangedEvent>(HandlePowerChange);

        SubscribeLocalEvent<IdAtmComponent, ComponentInit>(OnMoneyHolderComponentInit);
        SubscribeLocalEvent<IdAtmComponent, ComponentRemove>(OnMoneyHolderComponentRemove);
        SubscribeLocalEvent<IdAtmComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<IdAtmComponent, EntInsertedIntoContainerMessage>(OnIdAtmItemInserted);
        SubscribeLocalEvent<IdAtmComponent, EntRemovedFromContainerMessage>(OnIdAtmItemRemoved);
        SubscribeLocalEvent<IdAtmComponent, IdAtmUiMessageEvent>(OnIdAtmAction);
        SubscribeLocalEvent<IdAtmComponent, PinActionMessageEvent>(OnRequestUnlock);
    }

    private void OnRequestUnlock(EntityUid uid, IdAtmComponent component, PinActionMessageEvent args)
    {
        if (!TryComp<CreditCardComponent>(component.IdSlot.Item, out var cardComponent))
            return;
        switch (args.Action)
        {
            case IdAtmPinAction.Unlock:
                if (args.PinAttempt == cardComponent.Pin)
                {
                    cardComponent.Locked = false;
                }
                break;
            case IdAtmPinAction.Lock:
                cardComponent.Locked = true;
                break;
            case IdAtmPinAction.Change:
                cardComponent.Pin = args.PinAttempt;
                break;
        }
        UpdateUserInterface(uid, component);
    }

    private void OnIdAtmItemRemoved(EntityUid uid, IdAtmComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnIdAtmItemInserted(EntityUid uid, IdAtmComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnIdAtmAction(EntityUid uid, IdAtmComponent component, IdAtmUiMessageEvent args)
    {
        // UID here is the ATM
        if (!component.IdSlot.HasItem)
            return;

        if (!TryComp<CreditCardComponent>(component.IdSlot.Item, out var creditCardComponent))
            return;

        int amount;
        UpdateUserInterface(uid, component);

        switch (args.Action)
        {
            case IdAtmUiAction.Withdrawal:
                amount = Math.Abs(args.Amount);
                if (creditCardComponent.Balance >= amount)
                {
                    creditCardComponent.Balance -= amount;

                    var money = _stackSystem.Spawn(amount, _prototypeManager.Index<StackPrototype>("Credit"), Transform(uid).Coordinates);
                    _itemSlotsSystem.TryInsert(uid, component.MoneySlot, money, null);
                    UpdateUserInterface(uid, component);

                    break;
                }
                _popupSystem.PopupEntity(Loc.GetString("outpost-atm-component-popup-insufficient-balance"), uid);
                break;
            case IdAtmUiAction.Deposit:
                // completely ignore action.Amount
                var credits = component.MoneySlot.Item;
                if (!TryComp<StackComponent>(credits, out var stackComponent))
                    break;
                amount = stackComponent.Count;
                creditCardComponent.Balance += amount;
                _itemSlotsSystem.TryEject(uid, component.MoneySlot, null, out var item);
                if (item != null)
                    QueueDel(item.Value);
                else
                    QueueDel(credits.Value);
                UpdateUserInterface(uid, component);

                break;
        }
        UpdateUserInterface(uid, component);
    }

    private void OnToggleInterface(EntityUid uid, IdAtmComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, IdAtmComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var bank = 0;
        var cash = 0;
        string? name = null;

        if (TryComp<CreditCardComponent>(component.IdSlot.Item, out var cardComponent))
        {
            bank = cardComponent.Balance;
            if (TryComp<IdCardComponent>(component.IdSlot.Item, out var idCardComponent))
                name = idCardComponent.FullName;
        }

        if (TryComp<StackComponent>(component.MoneySlot.Item, out var stackComponent))
            cash = stackComponent.Count;

        Log.Debug("id in:" + component.IdSlot.HasItem);

        var state = new IdAtmUiState(name == null ? "John Doe" : name, bank, cash, component.IdSlot.HasItem, cardComponent?.Locked ?? false);
        _userInterfaceSystem.TrySetUiState(uid, IdAtmUiKey.Key, state);
    }

    private void OnMoneyHolderComponentRemove(EntityUid uid, IdAtmComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.MoneySlot);
        _itemSlotsSystem.RemoveItemSlot(uid, component.IdSlot);
    }

    private void OnMoneyHolderComponentInit(EntityUid uid, IdAtmComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, component.MoneyContainerId, component.MoneySlot);
        _itemSlotsSystem.AddItemSlot(uid, component.IdContainerId, component.IdSlot);
    }

    //
    // private void UpdateUiState(EntityUid uid, EntityUid loaderUid, PdaAtmCartridgeComponent? component)
    // {
    //     if (!Resolve(uid, ref component))
    //         return;
    //
    //     if (!TryComp<PdaComponent>(loaderUid, out var pda))
    //         return;
    //
    //     var state = new PdaAtmUiState(false, 0, 0);
    //
    //     if (!TryComp<MoneyHolderComponent>(loaderUid, out var moneyHolderComponent))
    //         return;
    //
    //     var pdaBalance = 0;
    //
    //     if (moneyHolderComponent.MoneySlot.Item.HasValue)
    //     {
    //         if (TryComp<StackComponent>(moneyHolderComponent.MoneySlot.Item.Value, out var stackComponent))
    //             pdaBalance = stackComponent.Count;
    //     }
    //
    //     if (pda.IdSlot.Item.HasValue)
    //     {
    //         if (TryComp<CreditCardComponent>(pda.IdSlot.Item, out var creditCardComponent))
    //         {
    //             state = new PdaAtmUiState(true, creditCardComponent.Balance, pdaBalance);
    //         }
    //     }
    //
    //     Logger.Debug(state.IdCardIn.ToString());
    //
    //     _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    // }

    // private void OnUiReady(EntityUid uid, PdaAtmCartridgeComponent component, CartridgeUiReadyEvent args)
    // {
    //     UpdateUiState(uid, args.Loader, component);
    // }

    #region Outpost ATM

    private void OnOutpostATMExaminedEvent(EntityUid uid, OutpostATMComponent component, ExaminedEvent args)
    {
        var xform = Transform(uid);
        if (!xform.GridUid.HasValue)
            return;
        var station = _stationSystem.GetOwningStation(xform.GridUid.Value, xform);
        if (!TryComp<StationBankAccountComponent>(station, out var bankAccountComponent))
            return;
        args.PushMarkup(Loc.GetString("outpost-atm-component-examine-message",
            ("credits", bankAccountComponent.Balance)));
    }

    private void HandlePowerChange(EntityUid uid, OutpostATMComponent component, ref PowerChangedEvent args)
    {
        component.Enabled = args.Powered;
    }

    private void OnOutpostATMAfterInteract(EntityUid uid, CreditComponent component, AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !TryComp<OutpostATMComponent>(args.Target, out var atmComponent))
            return;
        if (!TryComp<StackComponent>(uid, out var stackComponent))
            return;
        var xform = Transform(args.Target.Value);
        if (!atmComponent.Enabled || !xform.GridUid.HasValue)
            return;
        var station = _stationSystem.GetOwningStation(xform.GridUid.Value, xform);
        if (!TryComp<StationBankAccountComponent>(station, out var bankAccountComponent))
            return;
        _cargoSystem.UpdateBankAccount(station.Value, bankAccountComponent, stackComponent.Count);
        _popupSystem.PopupCoordinates(
            Loc.GetString("outpost-atm-component-popup-successful-deposit",
                ("credits", stackComponent.Count.ToString())), xform.Coordinates);
        QueueDel(uid);
    }

    private void OnOutpostATMGetVerbs(EntityUid uid, OutpostATMComponent component,
        GetVerbsEvent<ActivationVerb> args)
    {
        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;
        if (!actor.PlayerSession.AttachedEntity.HasValue)
            return;
        var xform = Transform(uid);
        if (!component.Enabled || !xform.Anchored || !xform.GridUid.HasValue)
            return;
        var station = _stationSystem.GetOwningStation(xform.GridUid.Value, xform);
        if (!TryComp<StationBankAccountComponent>(station, out var bankAccountComponent))
            return;

        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("outpost-atm-component-verb-withdraw"),
            Act = () =>
            {
                _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("outpost-atm-component-verb-withdraw"),
                    "Amount",
                    (int amount) =>
                    {
                        amount = Math.Abs(amount);
                        if (bankAccountComponent.Balance >= amount)
                        {
                            _cargoSystem.UpdateBankAccount(station.Value, bankAccountComponent, -amount);
                            _popupSystem.PopupEntity(
                                Loc.GetString("outpost-atm-component-popup-successful-withdrawal",
                                    ("credits", amount)), uid);

                            var money = _stackSystem.Spawn(amount,
                                _prototypeManager.Index<StackPrototype>("Credit"),
                                Transform(args.User).Coordinates);
                            if (TryComp<HandsComponent>(actor.PlayerSession.AttachedEntity.Value,
                                    out var handsComponent) && handsComponent.ActiveHand != null)
                                _handsSystem.DoPickup(actor.PlayerSession.AttachedEntity.Value,
                                    handsComponent.ActiveHand, money);

                            return;
                        }

                        _popupSystem.PopupEntity(Loc.GetString("outpost-atm-component-popup-insufficient-balance"),
                            uid);
                    });
            }
        });
    }
    #endregion
}
