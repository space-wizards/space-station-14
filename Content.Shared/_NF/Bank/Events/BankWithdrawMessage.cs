using Robust.Shared.Serialization;

namespace Content.Shared.Bank.Events;

/// <summary>
/// Raised on a client bank withdrawl
/// </summary>
[Serializable, NetSerializable]

public sealed class BankWithdrawMessage : BoundUserInterfaceMessage
{
    public int Amount; //amount to withdraw

    public BankWithdrawMessage(int amount)
    {
        Amount = amount;
    }
}
