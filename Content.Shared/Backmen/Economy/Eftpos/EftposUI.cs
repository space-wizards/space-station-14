// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Backmen.Economy.Eftpos;

[Serializable, NetSerializable]
public enum EftposUiKey : byte
{
    Key
}
[Serializable, NetSerializable]
public sealed class EftposChangeValueMessage : BoundUserInterfaceMessage
{
    public FixedPoint2? Value;
    public EftposChangeValueMessage(FixedPoint2? value)
    {
        Value = value;
    }
}
[Serializable, NetSerializable]
public sealed class EftposChangeLinkedAccountNumberMessage : BoundUserInterfaceMessage
{
    public string? LinkedAccountNumber;
    public EftposChangeLinkedAccountNumberMessage(string? linkedAccountNumber)
    {
        LinkedAccountNumber = linkedAccountNumber;
    }
}

[Serializable, NetSerializable]
public sealed class EftposSwipeCardMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class EftposLockMessage : BoundUserInterfaceMessage { }
