using Content.Shared.Bank.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Bank;

[NetSerializable, Serializable]
public enum BankATMMenuUiKey : byte
{
    ATM
}

public sealed partial class SharedBankSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BankAccountComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<BankATMComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BankATMComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, BankATMComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, BankATMComponent.CashSlotSlotId, component.CashSlot);
    }

    private void OnComponentRemove(EntityUid uid, BankATMComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.CashSlot);
    }

    private void OnHandleState(EntityUid playerUid, BankAccountComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BankAccountComponentState state)
        {
            return;
        }

        component.Balance = state.Balance;
    }
}

