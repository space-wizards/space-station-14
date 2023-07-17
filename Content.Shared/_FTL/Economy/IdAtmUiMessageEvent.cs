using Robust.Shared.Serialization;

namespace Content.Shared._FTL.Economy;

[Serializable, NetSerializable]
public sealed class IdAtmUiMessageEvent : BoundUserInterfaceMessage
{
    public readonly IdAtmUiAction Action;
    public readonly int Amount;
    public readonly EntityUid Entity;

    public IdAtmUiMessageEvent(EntityUid entity, IdAtmUiAction action, int amount)
    {
        Entity = entity;
        Action = action;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class PinActionMessageEvent : BoundUserInterfaceMessage
{
    public readonly IdAtmPinAction Action;
    public readonly string PinAttempt;

    public PinActionMessageEvent(IdAtmPinAction action, string pinAttempt)
    {
        Action = action;
        PinAttempt = pinAttempt;
    }
}

[Serializable, NetSerializable]
public enum IdAtmUiAction
{
    Withdrawal,
    Deposit,
    Lock
}

[Serializable, NetSerializable]
public enum IdAtmPinAction
{
    Unlock,
    Lock,
    Change
}

[NetSerializable, Serializable]
public enum IdAtmUiKey : byte
{
    Key,
}
