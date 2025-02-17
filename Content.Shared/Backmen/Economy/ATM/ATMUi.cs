// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Backmen.Economy.ATM;

[Serializable, NetSerializable]
public enum ATMUiKey : byte
{
    Key
}
[Serializable, NetSerializable]
public sealed class ATMRequestWithdrawMessage : BoundUserInterfaceMessage
{
    public FixedPoint2 Amount;
    public string AccountPin;
    public ATMRequestWithdrawMessage(FixedPoint2 amount, string accountPin)
    {
        Amount = amount;
        AccountPin = accountPin;
    }
}
