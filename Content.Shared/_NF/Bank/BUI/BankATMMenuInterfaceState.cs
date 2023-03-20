using Robust.Shared.Serialization;

namespace Content.Shared.Bank.BUI;

[NetSerializable, Serializable]
public sealed class BankATMMenuInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// bank balance of the character using the atm
    /// </summary>
    public int Balance;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// how much cash is inserted
    /// </summary>
    public int Deposit;

    public BankATMMenuInterfaceState(int balance, bool enabled, int deposit)
    {
        Balance = balance;
        Enabled = enabled;
        Deposit = deposit;
    }
}
