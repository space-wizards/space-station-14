using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Lock;

public enum DigitalLockStatus : byte
{
    AWAIT_CODE,
    AWAIT_CONFIRMATION,
    OPENED,
    CHANGE_MODE_CONFIRMATION,
    CHANGE_MODE_CODE,
    CHANGE_MODE_CANCEL_CONFIRMATION
}

[Serializable, NetSerializable]
public sealed class DigitalLockUiState : BoundUserInterfaceState
{
    public DigitalLockStatus Status;
    public int EnteredCodeLength;
    public int MaxCodeLength;
}

[Serializable, NetSerializable]
public enum DigitalLockUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DigitalLockKeypadMessage : BoundUserInterfaceMessage
{
    public int Value;

    public DigitalLockKeypadMessage(int value) => Value = value;
}

[Serializable, NetSerializable]
public sealed class DigitalLockKeypadClearMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DigitalLockKeypadEnterMessage : BoundUserInterfaceMessage
{
}