using Robust.Shared.Serialization;

namespace Content.Shared.Bank.Events;

/// <summary>
/// Raised on a client bank withdrawl
/// </summary>
[Serializable, NetSerializable]

public sealed class BankWithdrawMessage : BoundUserInterfaceMessage
{
    //amount to withdraw. validation is happening server side but we still need client input from a text field.
    public int Amount;

    public BankWithdrawMessage(int amount)
    {
        Amount = amount;
    }
}
