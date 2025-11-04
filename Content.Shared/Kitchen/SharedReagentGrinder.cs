using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen;

[Serializable, NetSerializable]
public sealed class ReagentGrinderToggleAutoModeMessage() : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ReagentGrinderStartMessage(GrinderProgram program) : BoundUserInterfaceMessage
{
    public GrinderProgram Program = program;
}

[Serializable, NetSerializable]
public sealed class ReagentGrinderEjectChamberAllMessage() : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ReagentGrinderEjectChamberContentMessage(NetEntity entityId) : BoundUserInterfaceMessage
{
    public NetEntity EntityId = entityId;
}

[Serializable, NetSerializable]
public enum ReagentGrinderVisualState : byte
{
    BeakerAttached
}

[Serializable, NetSerializable]
public enum GrinderProgram : byte
{
    Grind,
    Juice
}

[NetSerializable, Serializable]
public enum ReagentGrinderUiKey : byte
{
    Key
}

[NetSerializable, Serializable]
public enum GrinderAutoMode : byte
{
    Off,
    Grind,
    Juice
}
