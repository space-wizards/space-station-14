using Robust.Shared.Serialization;

namespace Content.Shared.Bank.Events;

/// <summary>
/// Raised on a client bank deposit
/// </summary>
[Serializable, NetSerializable]

public sealed class BankDepositMessage : BoundUserInterfaceMessage
{
    // an empty message because we dont really want clients to be able to send funny ints to deposit
    public BankDepositMessage()
    {
    }
}
