using Robust.Shared.GameStates;
using Content.Shared.Bank.Components;

namespace Content.Shared.Bank;

public sealed class SharedBankSystem : EntitySystem
{
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BankAccountComponent, ComponentHandleState>(OnHandleState);
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

