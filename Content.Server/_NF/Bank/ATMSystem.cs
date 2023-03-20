using Content.Server.Stack;
using Content.Shared.Bank.BUI;
using Content.Shared.Stacks;
using Content.Shared.Bank.Events;
using Content.Shared.Bank;
using Content.Shared.Coordinates;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Players;
using Content.Shared.Bank.Components;
using Content.Shared.Shipyard.Components;
using Robust.Shared.Containers;
using Content.Shared.Shipyard;
using System.Linq;

namespace Content.Server.Bank;

public sealed partial class BankSystem
{
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    private void InitializeATM()
    {
        SubscribeLocalEvent<BankATMComponent, BankWithdrawMessage>(OnWithdraw);
        SubscribeLocalEvent<BankATMComponent, BankDepositMessage>(OnDeposit);
        SubscribeLocalEvent<BankATMComponent, BoundUIOpenedEvent>(OnATMUIOpen);
        SubscribeLocalEvent<BankATMComponent, EntInsertedIntoContainerMessage>(OnCashSlotChanged);
        SubscribeLocalEvent<BankATMComponent, EntRemovedFromContainerMessage>(OnCashSlotChanged);
    }

    private void GetInsertedCashAmount(BankATMComponent component, out int amount)
    {
        amount = 0;
        var cashEntity = component.CashSlot.ContainerSlot?.ContainedEntity;

        if (!TryComp<StackComponent>(cashEntity, out var cashStack))
        {
            return;
        }

        if (cashStack.StackTypeId != component.CashType)
        {
            return;
        }

        amount = cashStack.Count;
        return;
    }
    private void OnWithdraw(EntityUid uid, BankATMComponent component, BankWithdrawMessage args)
    {

        if (args.Session.AttachedEntity is not { Valid : true } player)
            return;

        GetInsertedCashAmount(component, out var deposit);
        var bui = _uiSystem.GetUi(component.Owner, BankATMMenuUiKey.ATM);
        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            _log.Info($"{player} has no bank account");
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        if (!TryBankWithdraw(player, args.Amount))
        {
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(bank.Balance, true, deposit));
            return;
        }
        var stackPrototype = _prototypeManager.Index<StackPrototype>(component.CashType);
        _stackSystem.Spawn(args.Amount, stackPrototype, uid.ToCoordinates());
        _uiSystem.SetUiState(bui,
            new BankATMMenuInterfaceState(bank.Balance, true, deposit));
    }

    private void OnDeposit(EntityUid uid, BankATMComponent component, BankDepositMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        GetInsertedCashAmount(component, out var deposit);
        var bui = _uiSystem.GetUi(component.Owner, BankATMMenuUiKey.ATM);
        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            _log.Info($"{player} has no bank account");
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        if (component.CashSlot.ContainerSlot is not IContainer cashSlot)
        {
            _log.Info($"ATM has no cash slot");
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        if (!TryBankDeposit(player, deposit))
        {
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(bank.Balance, true, deposit));
            return;
        }
        _containerSystem.CleanContainer(cashSlot);
        _uiSystem.SetUiState(bui,
            new BankATMMenuInterfaceState(bank.Balance, true, 0));
        return;
    }
    private void OnCashSlotChanged(EntityUid uid, BankATMComponent component, ContainerModifiedMessage args)
    {

        // kind of cursed. We need to update the UI when cash is entered, but the UI needs to know the player characters bank account.
        var bankUi = _uiSystem.GetUi(uid, BankATMMenuUiKey.ATM);
        var uiUser = bankUi.SubscribedSessions.FirstOrDefault();
        GetInsertedCashAmount(component, out var deposit);

        if (uiUser?.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            return;
        }

        if (component.CashSlot.ContainerSlot?.ContainedEntity is not { Valid : true } cash)
        {
            _uiSystem.SetUiState(bankUi,
                new BankATMMenuInterfaceState(bank.Balance, true, 0));
        }

        _uiSystem.SetUiState(bankUi,
            new BankATMMenuInterfaceState(bank.Balance, true, deposit));
    }

    private void OnATMUIOpen(EntityUid uid, BankATMComponent component, BoundUIOpenedEvent args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;
        GetInsertedCashAmount(component, out var deposit);
        var bui = _uiSystem.GetUi(component.Owner, BankATMMenuUiKey.ATM);
        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            _log.Info($"{player} has no bank account");
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }
        _uiSystem.SetUiState(bui,
            new BankATMMenuInterfaceState(bank.Balance, true, deposit));
    }
}
