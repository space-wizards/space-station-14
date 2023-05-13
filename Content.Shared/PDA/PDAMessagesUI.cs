using Robust.Shared.Serialization;

namespace Content.Shared.PDA;

[Serializable, NetSerializable]
public sealed class PDAToggleFlashlightMessage : BoundUserInterfaceMessage
{
    public PDAToggleFlashlightMessage() { }
}

[Serializable, NetSerializable]
public sealed class PDAShowRingtoneMessage : BoundUserInterfaceMessage
{
    public PDAShowRingtoneMessage() { }
}

[Serializable, NetSerializable]
public sealed class PDAShowUplinkMessage : BoundUserInterfaceMessage
{
    public PDAShowUplinkMessage() { }
}

[Serializable, NetSerializable]
public sealed class PDALockUplinkMessage : BoundUserInterfaceMessage
{
    public PDALockUplinkMessage() { }
}

[Serializable, NetSerializable]
public sealed class PDAShowMusicMessage : BoundUserInterfaceMessage
{
    public PDAShowMusicMessage() { }
}

[Serializable, NetSerializable]
public sealed class PDARequestUpdateInterfaceMessage : BoundUserInterfaceMessage
{
    public PDARequestUpdateInterfaceMessage()  { }
}
